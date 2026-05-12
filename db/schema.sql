-- ============================================================================
--  Supabase (PostgreSQL) Schema  -  Professional Accounting System
--  Target: PostgreSQL 15+ (Supabase)
--  Conventions:
--    * All monetary values        : numeric(18, 4)
--    * All exchange rates         : numeric(18, 8)
--    * Primary keys               : uuid (gen_random_uuid())
--    * Timestamps                 : timestamptz, default now()
--    * Immutable audit trail      : hard deletes are forbidden; corrections are
--                                   recorded via "Reversal Entries" only.
--    * Multi-tenant ready         : every fact table carries org_id (uuid).
--    * Row Level Security         : enabled on all business tables.
-- ============================================================================

create extension if not exists "pgcrypto";
create extension if not exists "uuid-ossp";
create extension if not exists "pg_trgm";

-- ---------------------------------------------------------------------------
-- 0. Schemas
-- ---------------------------------------------------------------------------
create schema if not exists accounting;
set search_path to accounting, public;

-- ---------------------------------------------------------------------------
-- 1. Enumerations
-- ---------------------------------------------------------------------------
do $$ begin
    create type account_type    as enum ('asset','liability','equity','revenue','expense');
    create type normal_balance  as enum ('debit','credit');
    create type entry_status    as enum ('draft','posted','reversed','reversal');
    create type fiscal_status   as enum ('open','locked','closed');
    create type sync_state      as enum ('pending','synced','conflict','error');
exception when duplicate_object then null; end $$;

-- ---------------------------------------------------------------------------
-- 2. Organisations & Users  (multi-tenant container)
-- ---------------------------------------------------------------------------
create table if not exists organisations (
    id              uuid primary key default gen_random_uuid(),
    name            text not null,
    base_currency   char(3) not null default 'USD',
    fiscal_start    smallint not null default 1   check (fiscal_start between 1 and 12),
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

-- Mapping of Supabase auth.users to organisation roles.
create table if not exists organisation_members (
    org_id          uuid not null references organisations(id) on delete cascade,
    user_id         uuid not null,                          -- = auth.users.id
    role            text not null default 'accountant'      -- owner|admin|accountant|viewer
                    check (role in ('owner','admin','accountant','viewer')),
    created_at      timestamptz not null default now(),
    primary key (org_id, user_id)
);

-- ---------------------------------------------------------------------------
-- 3. Currencies & Historical Exchange Rates
-- ---------------------------------------------------------------------------
create table if not exists currencies (
    code            char(3) primary key,            -- ISO-4217
    name            text not null,
    symbol          text,
    decimals        smallint not null default 2,
    is_active       boolean not null default true
);

create table if not exists exchange_rates (
    id              uuid primary key default gen_random_uuid(),
    org_id          uuid not null references organisations(id) on delete cascade,
    from_currency   char(3) not null references currencies(code),
    to_currency     char(3) not null references currencies(code),
    rate            numeric(18,8) not null check (rate > 0),
    rate_date       date not null,
    source          text default 'manual',
    created_at      timestamptz not null default now(),
    unique (org_id, from_currency, to_currency, rate_date)
);
create index if not exists ix_exchange_rates_lookup
    on exchange_rates (org_id, from_currency, to_currency, rate_date desc);

-- ---------------------------------------------------------------------------
-- 4. Fiscal Years & Period Locking
-- ---------------------------------------------------------------------------
create table if not exists fiscal_years (
    id              uuid primary key default gen_random_uuid(),
    org_id          uuid not null references organisations(id) on delete cascade,
    code            text not null,                  -- e.g. "FY-2026"
    start_date      date not null,
    end_date        date not null,
    status          fiscal_status not null default 'open',
    closed_at       timestamptz,
    closed_by       uuid,                           -- auth.users.id
    retained_earnings_account uuid,                 -- set on close
    unique (org_id, code),
    check  (end_date > start_date)
);

create table if not exists fiscal_periods (
    id              uuid primary key default gen_random_uuid(),
    fiscal_year_id  uuid not null references fiscal_years(id) on delete cascade,
    code            text not null,                  -- e.g. "2026-M01"
    start_date      date not null,
    end_date        date not null,
    status          fiscal_status not null default 'open',
    unique (fiscal_year_id, code)
);
create index if not exists ix_fiscal_periods_dates
    on fiscal_periods (fiscal_year_id, start_date, end_date);

-- ---------------------------------------------------------------------------
-- 5. Cost Centers
-- ---------------------------------------------------------------------------
create table if not exists cost_centers (
    id              uuid primary key default gen_random_uuid(),
    org_id          uuid not null references organisations(id) on delete cascade,
    code            text not null,
    name            text not null,
    parent_id       uuid references cost_centers(id),
    is_active       boolean not null default true,
    created_at      timestamptz not null default now(),
    unique (org_id, code)
);

-- ---------------------------------------------------------------------------
-- 6. Chart of Accounts  (recursive tree)
-- ---------------------------------------------------------------------------
create table if not exists accounts (
    id              uuid primary key default gen_random_uuid(),
    org_id          uuid not null references organisations(id) on delete cascade,
    code            text not null,                  -- hierarchical e.g. "1.1.01"
    name            text not null,
    name_ar         text,                           -- Arabic name (RTL UI)
    account_type    account_type not null,
    normal_balance  normal_balance not null,
    parent_id       uuid references accounts(id),
    currency        char(3) not null default 'USD' references currencies(code),
    is_postable     boolean not null default true,  -- false for header/group rows
    is_active       boolean not null default true,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),
    unique (org_id, code)
);
create index if not exists ix_accounts_parent on accounts (parent_id);
create index if not exists ix_accounts_type   on accounts (org_id, account_type);

-- Recursive view: account path & depth (handy for trees / aggregation)
create or replace view v_accounts_tree as
with recursive tree as (
    select a.id, a.org_id, a.code, a.name, a.parent_id,
           a.account_type, a.normal_balance, a.is_postable,
           1 as depth,
           a.code::text as path
    from   accounts a
    where  a.parent_id is null
    union all
    select c.id, c.org_id, c.code, c.name, c.parent_id,
           c.account_type, c.normal_balance, c.is_postable,
           t.depth + 1,
           t.path || ' > ' || c.code
    from   accounts c
    join   tree t on t.id = c.parent_id
)
select * from tree;

-- ---------------------------------------------------------------------------
-- 7. Journal Headers & Lines  (double entry, immutable)
-- ---------------------------------------------------------------------------
create table if not exists journal_headers (
    id              uuid primary key default gen_random_uuid(),
    org_id          uuid not null references organisations(id) on delete cascade,
    entry_no        text not null,                  -- per-org running number
    entry_date      date not null,
    description     text,
    reference       text,
    currency        char(3) not null references currencies(code),
    exchange_rate   numeric(18,8) not null default 1 check (exchange_rate > 0),
    fiscal_period_id uuid references fiscal_periods(id),
    status          entry_status not null default 'draft',
    reversed_by_id  uuid references journal_headers(id),
    reverses_id     uuid references journal_headers(id),
    posted_at       timestamptz,
    posted_by       uuid,                           -- auth.users.id
    created_at      timestamptz not null default now(),
    created_by      uuid,                           -- auth.users.id
    unique (org_id, entry_no)
);
create index if not exists ix_journal_headers_date
    on journal_headers (org_id, entry_date);
create index if not exists ix_journal_headers_status
    on journal_headers (org_id, status);

create table if not exists journal_lines (
    id              uuid primary key default gen_random_uuid(),
    header_id       uuid not null references journal_headers(id) on delete cascade,
    line_no         smallint not null,
    account_id      uuid not null references accounts(id),
    cost_center_id  uuid references cost_centers(id),
    description     text,
    debit           numeric(18,4) not null default 0 check (debit  >= 0),
    credit          numeric(18,4) not null default 0 check (credit >= 0),
    -- Base-currency amounts (computed at posting time using header.exchange_rate)
    debit_base      numeric(18,4) not null default 0,
    credit_base     numeric(18,4) not null default 0,
    unique (header_id, line_no),
    check ( (debit = 0 and credit > 0) or (debit > 0 and credit = 0) )
);
create index if not exists ix_journal_lines_account on journal_lines (account_id);

-- ---------------------------------------------------------------------------
-- 8. Triggers: immutability, balance validation, period lock
-- ---------------------------------------------------------------------------

-- 8.1 Forbid hard deletes on posted entries
create or replace function tg_no_delete_posted() returns trigger as $$
begin
    if old.status in ('posted','reversed') then
        raise exception 'POSTED_ENTRY_IMMUTABLE: use a reversal entry instead (id=%)', old.id;
    end if;
    return old;
end $$ language plpgsql;

drop trigger if exists trg_no_delete_posted on journal_headers;
create trigger trg_no_delete_posted
before delete on journal_headers
for each row execute function tg_no_delete_posted();

-- 8.2 Forbid edits to posted entries (except controlled status transitions)
create or replace function tg_no_update_posted() returns trigger as $$
begin
    if old.status = 'posted' and new.status not in ('posted','reversed') then
        return new;  -- allow controlled transition
    end if;
    if old.status in ('posted','reversed')
       and (new.entry_date <> old.entry_date
         or new.currency  <> old.currency
         or new.exchange_rate <> old.exchange_rate)
    then
        raise exception 'POSTED_ENTRY_IMMUTABLE: cannot modify financial fields (id=%)', old.id;
    end if;
    return new;
end $$ language plpgsql;

drop trigger if exists trg_no_update_posted on journal_headers;
create trigger trg_no_update_posted
before update on journal_headers
for each row execute function tg_no_update_posted();

-- 8.3 Validate Σdebit = Σcredit before allowing 'posted'
create or replace function tg_validate_balanced_entry() returns trigger as $$
declare
    v_sum_debit  numeric(18,4);
    v_sum_credit numeric(18,4);
begin
    if new.status = 'posted' and (old.status is null or old.status <> 'posted') then
        select coalesce(sum(debit),0), coalesce(sum(credit),0)
          into v_sum_debit, v_sum_credit
          from journal_lines
         where header_id = new.id;

        if v_sum_debit <> v_sum_credit then
            raise exception
              'UNBALANCED_ENTRY: debit=% credit=% (id=%)',
              v_sum_debit, v_sum_credit, new.id;
        end if;

        if v_sum_debit = 0 then
            raise exception 'EMPTY_ENTRY: at least one non-zero line required (id=%)', new.id;
        end if;

        new.posted_at := coalesce(new.posted_at, now());
    end if;
    return new;
end $$ language plpgsql;

drop trigger if exists trg_validate_balanced_entry on journal_headers;
create trigger trg_validate_balanced_entry
before update on journal_headers
for each row execute function tg_validate_balanced_entry();

-- 8.4 Block posting into a locked/closed fiscal period
create or replace function tg_block_locked_period() returns trigger as $$
declare
    v_status fiscal_status;
begin
    if new.status = 'posted' then
        select fp.status into v_status
          from fiscal_periods fp
         where fp.id = new.fiscal_period_id;

        if v_status in ('locked','closed') then
            raise exception 'PERIOD_LOCKED: cannot post into a % period (id=%)', v_status, new.id;
        end if;
    end if;
    return new;
end $$ language plpgsql;

drop trigger if exists trg_block_locked_period on journal_headers;
create trigger trg_block_locked_period
before update on journal_headers
for each row execute function tg_block_locked_period();

-- 8.5 Compute base-currency amounts on line insert/update
create or replace function tg_compute_base_amounts() returns trigger as $$
declare
    v_rate numeric(18,8);
begin
    select exchange_rate into v_rate
      from journal_headers
     where id = new.header_id;

    new.debit_base  := round(new.debit  * v_rate, 4);
    new.credit_base := round(new.credit * v_rate, 4);
    return new;
end $$ language plpgsql;

drop trigger if exists trg_compute_base_amounts on journal_lines;
create trigger trg_compute_base_amounts
before insert or update on journal_lines
for each row execute function tg_compute_base_amounts();

-- ---------------------------------------------------------------------------
-- 9. Audit Log (append-only)
-- ---------------------------------------------------------------------------
create table if not exists audit_log (
    id              bigserial primary key,
    org_id          uuid,
    user_id         uuid,
    table_name      text not null,
    record_id       text not null,
    action          text not null,                  -- INSERT|UPDATE|DELETE|POST|REVERSE
    diff            jsonb,
    created_at      timestamptz not null default now()
);
create index if not exists ix_audit_log_record
    on audit_log (table_name, record_id, created_at desc);

create or replace function tg_audit_row() returns trigger as $$
declare
    v_diff jsonb;
    v_id   text;
    v_org  uuid;
begin
    if tg_op = 'DELETE' then
        v_diff := to_jsonb(old);
        v_id   := old.id::text;
        v_org  := (case when to_jsonb(old) ? 'org_id' then (to_jsonb(old)->>'org_id')::uuid end);
    else
        v_diff := jsonb_build_object('old', to_jsonb(old), 'new', to_jsonb(new));
        v_id   := new.id::text;
        v_org  := (case when to_jsonb(new) ? 'org_id' then (to_jsonb(new)->>'org_id')::uuid end);
    end if;

    insert into audit_log(org_id, user_id, table_name, record_id, action, diff)
    values (v_org, auth.uid(), tg_table_name, v_id, tg_op, v_diff);

    return coalesce(new, old);
end $$ language plpgsql security definer;

-- Attach audit trigger to all business tables
do $$
declare t text;
begin
    foreach t in array array[
        'accounts','journal_headers','journal_lines',
        'currencies','exchange_rates','cost_centers',
        'fiscal_years','fiscal_periods'
    ] loop
        execute format('drop trigger if exists trg_audit_%1$s on %1$I', t);
        execute format('create trigger trg_audit_%1$s
                        after insert or update or delete on %1$I
                        for each row execute function tg_audit_row()', t);
    end loop;
end $$;

-- ---------------------------------------------------------------------------
-- 10. Reporting views
-- ---------------------------------------------------------------------------

-- 10.1 Trial Balance (base currency)
create or replace view v_trial_balance as
select a.org_id,
       a.id            as account_id,
       a.code          as account_code,
       a.name          as account_name,
       a.account_type,
       sum(jl.debit_base)  as total_debit,
       sum(jl.credit_base) as total_credit,
       sum(jl.debit_base) - sum(jl.credit_base) as balance
from   accounts a
left join journal_lines   jl on jl.account_id = a.id
left join journal_headers jh on jh.id = jl.header_id and jh.status = 'posted'
group by a.org_id, a.id, a.code, a.name, a.account_type;

-- 10.2 Account balances with parent rollup (recursive)
create or replace view v_account_balances_rollup as
with recursive tb as (
    select account_id, org_id, balance from v_trial_balance
),
walk as (
    select a.id as root_id, a.id as node_id, tb.balance, a.org_id
      from accounts a left join tb on tb.account_id = a.id
    union all
    select w.root_id, c.id, coalesce(tb.balance, 0), c.org_id
      from walk w join accounts c on c.parent_id = w.node_id
      left join tb on tb.account_id = c.id
)
select org_id, root_id as account_id, sum(balance) as rollup_balance
  from walk
 group by org_id, root_id;

-- ---------------------------------------------------------------------------
-- 11. Sync Outbox  (used by the WPF client's SyncService)
-- ---------------------------------------------------------------------------
create table if not exists sync_outbox (
    id              uuid primary key default gen_random_uuid(),
    org_id          uuid not null references organisations(id) on delete cascade,
    table_name      text not null,
    record_id       text not null,
    payload         jsonb not null,
    state           sync_state not null default 'pending',
    attempts        smallint not null default 0,
    last_error      text,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);
create index if not exists ix_sync_outbox_state on sync_outbox (state, created_at);

-- ---------------------------------------------------------------------------
-- 12. Row Level Security
-- ---------------------------------------------------------------------------
alter table organisations         enable row level security;
alter table organisation_members  enable row level security;
alter table accounts              enable row level security;
alter table journal_headers       enable row level security;
alter table journal_lines         enable row level security;
alter table cost_centers          enable row level security;
alter table fiscal_years          enable row level security;
alter table fiscal_periods        enable row level security;
alter table exchange_rates        enable row level security;
alter table audit_log             enable row level security;
alter table sync_outbox           enable row level security;

-- Helper: is the caller a member of <org>?
create or replace function is_org_member(p_org uuid) returns boolean
language sql stable security definer as $$
    select exists (
        select 1 from organisation_members
         where org_id  = p_org
           and user_id = auth.uid()
    );
$$;

-- Helper: caller's role in <org>
create or replace function org_role(p_org uuid) returns text
language sql stable security definer as $$
    select role from organisation_members
     where org_id = p_org and user_id = auth.uid();
$$;

-- Generic policy applied to every org-scoped table
do $$
declare t text;
begin
    foreach t in array array[
        'accounts','journal_headers','journal_lines',
        'cost_centers','fiscal_years','fiscal_periods',
        'exchange_rates','sync_outbox'
    ] loop
        execute format('drop policy if exists p_select_%1$s on %1$I', t);
        execute format('create policy p_select_%1$s on %1$I
                        for select using (is_org_member(org_id))', t);

        execute format('drop policy if exists p_modify_%1$s on %1$I', t);
        execute format('create policy p_modify_%1$s on %1$I
                        for all using  (is_org_member(org_id)
                                        and org_role(org_id) in (''owner'',''admin'',''accountant''))
                                  with check (is_org_member(org_id)
                                        and org_role(org_id) in (''owner'',''admin'',''accountant''))', t);
    end loop;
end $$;

-- Journal lines inherit access via their header (no direct org_id column needed)
drop policy if exists p_select_journal_lines on journal_lines;
create policy p_select_journal_lines on journal_lines
    for select using (exists (
        select 1 from journal_headers jh
         where jh.id = journal_lines.header_id
           and is_org_member(jh.org_id)
    ));
drop policy if exists p_modify_journal_lines on journal_lines;
create policy p_modify_journal_lines on journal_lines
    for all using (exists (
        select 1 from journal_headers jh
         where jh.id = journal_lines.header_id
           and is_org_member(jh.org_id)
           and org_role(jh.org_id) in ('owner','admin','accountant')
    )) with check (exists (
        select 1 from journal_headers jh
         where jh.id = journal_lines.header_id
           and is_org_member(jh.org_id)
           and org_role(jh.org_id) in ('owner','admin','accountant')
    ));

-- Audit log: read-only for org members; never deletable/updatable from client
drop policy if exists p_select_audit on audit_log;
create policy p_select_audit on audit_log
    for select using (org_id is null or is_org_member(org_id));

-- Organisations / members: caller must be a member to see them
drop policy if exists p_select_orgs on organisations;
create policy p_select_orgs on organisations
    for select using (is_org_member(id));
drop policy if exists p_select_org_members on organisation_members;
create policy p_select_org_members on organisation_members
    for select using (is_org_member(org_id));

-- ---------------------------------------------------------------------------
-- 13. Year-End Closing  (Revenue + Expenses --> Retained Earnings)
-- ---------------------------------------------------------------------------
create or replace function close_fiscal_year(
    p_year_id   uuid,
    p_retained  uuid   -- accounts.id for "Retained Earnings"
) returns uuid
language plpgsql security definer as $$
declare
    v_org           uuid;
    v_end_date      date;
    v_currency      char(3);
    v_header_id     uuid := gen_random_uuid();
    v_net_income    numeric(18,4);
begin
    select fy.org_id, fy.end_date, o.base_currency
      into v_org, v_end_date, v_currency
      from fiscal_years fy
      join organisations o on o.id = fy.org_id
     where fy.id = p_year_id
       and fy.status = 'open';

    if v_org is null then
        raise exception 'FISCAL_YEAR_NOT_OPEN (%)', p_year_id;
    end if;

    -- Net income for the year (revenue - expense) in base currency
    select coalesce(sum(case when a.account_type = 'revenue'
                              then jl.credit_base - jl.debit_base
                              else jl.debit_base  - jl.credit_base end), 0)
      into v_net_income
      from journal_lines   jl
      join journal_headers jh on jh.id = jl.header_id and jh.status = 'posted'
      join accounts        a  on a.id  = jl.account_id
     where jh.org_id = v_org
       and jh.entry_date between
             (select start_date from fiscal_years where id = p_year_id)
         and v_end_date
       and a.account_type in ('revenue','expense');

    -- Closing entry header
    insert into journal_headers(
        id, org_id, entry_no, entry_date, description, currency,
        exchange_rate, status, posted_at, posted_by
    ) values (
        v_header_id, v_org,
        'CLOSE-' || to_char(v_end_date,'YYYY'),
        v_end_date,
        'Year-end closing entry',
        v_currency, 1, 'posted', now(), auth.uid()
    );

    -- One line per revenue/expense account zeroing out its balance,
    -- offset to Retained Earnings.
    insert into journal_lines(header_id, line_no, account_id, debit, credit)
    select v_header_id,
           row_number() over (order by a.code),
           a.id,
           case when bal < 0 then -bal else 0 end,
           case when bal > 0 then  bal else 0 end
      from (
        select a.id, a.code,
               sum(jl.debit_base - jl.credit_base) as bal
          from journal_lines jl
          join journal_headers jh on jh.id = jl.header_id and jh.status = 'posted'
          join accounts a on a.id = jl.account_id
         where jh.org_id = v_org
           and a.account_type in ('revenue','expense')
           and jh.entry_date <= v_end_date
         group by a.id, a.code
         having sum(jl.debit_base - jl.credit_base) <> 0
      ) t
      join accounts a on a.id = t.id;

    -- Offsetting line to Retained Earnings
    insert into journal_lines(header_id, line_no, account_id, debit, credit)
    values (
        v_header_id,
        (select coalesce(max(line_no),0) + 1 from journal_lines where header_id = v_header_id),
        p_retained,
        case when v_net_income < 0 then -v_net_income else 0 end,
        case when v_net_income > 0 then  v_net_income else 0 end
    );

    update fiscal_years
       set status = 'closed',
           closed_at = now(),
           closed_by = auth.uid(),
           retained_earnings_account = p_retained
     where id = p_year_id;

    update fiscal_periods set status = 'closed'
     where fiscal_year_id = p_year_id;

    return v_header_id;
end $$;

-- ---------------------------------------------------------------------------
-- 14. Seed: ISO currencies (sample)
-- ---------------------------------------------------------------------------
insert into currencies (code, name, symbol, decimals) values
    ('USD','US Dollar','$',2),
    ('EUR','Euro','€',2),
    ('GBP','British Pound','£',2),
    ('EGP','Egyptian Pound','E£',2),
    ('SAR','Saudi Riyal','﷼',2),
    ('AED','UAE Dirham','د.إ',2)
on conflict (code) do nothing;

-- ============================================================================
-- End of schema
-- ============================================================================
