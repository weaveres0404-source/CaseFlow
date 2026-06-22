-- Allow both legacy and new case type codes, then switch the default to REQUEST.
-- This keeps existing historical cases insertable/queryable while unblocking new-case creation.

ALTER TABLE public.cases
    DROP CONSTRAINT IF EXISTS cases_case_type_check;

ALTER TABLE public.cases
    ADD CONSTRAINT cases_case_type_check
    CHECK (
        (case_type)::text = ANY (
            ARRAY[
                ('REQUEST'::character varying)::text,
                ('DEVELOPMENT'::character varying)::text,
                ('TEST'::character varying)::text,
                ('UAT'::character varying)::text,
                ('GO_LIVE'::character varying)::text,
                ('UHD'::character varying)::text,
                ('MAINT'::character varying)::text,
                ('SUPPORT'::character varying)::text,
                ('INCIDENT'::character varying)::text,
                ('MEETING'::character varying)::text,
                ('RELEASE'::character varying)::text,
                ('UPGRADE'::character varying)::text,
                ('REPAIR'::character varying)::text,
                ('EVALUATION'::character varying)::text,
                ('MAINTENANCE'::character varying)::text,
                ('INQUIRY'::character varying)::text
            ]
        )
    );

ALTER TABLE public.cases
    ALTER COLUMN case_type SET DEFAULT 'REQUEST';
