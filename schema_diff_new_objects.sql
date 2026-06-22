-- Schema diff from backup_v2.sql to Cloud_SQL_Export_2026-05-29 (14_58_33).sql
-- Purpose: manually add new tables/columns introduced in the newer schema.
-- This script only changes structure. It does not import test data.

-- 1) New columns on existing tables
ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS google_sub character varying(64),
    ADD COLUMN IF NOT EXISTS google_email character varying(255),
    ADD COLUMN IF NOT EXISTS auth_provider character varying(20) NOT NULL DEFAULT 'local';

ALTER TABLE public.case_logs
    ADD COLUMN IF NOT EXISTS ref_case_id integer;

ALTER TABLE public.problem_categories
    ADD COLUMN IF NOT EXISTS project_id integer;

ALTER TABLE public.projects
    ADD COLUMN IF NOT EXISTS allowed_case_types text,
    ADD COLUMN IF NOT EXISTS project_code_id integer;

-- 2) New sequences
CREATE SEQUENCE IF NOT EXISTS public.case_types_type_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS public.case_type_projects_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS public.problem_category_case_types_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS public.problem_category_projects_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS public.project_codes_code_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- 3) New tables
CREATE TABLE IF NOT EXISTS public.case_types (
    type_id integer NOT NULL DEFAULT nextval('public.case_types_type_id_seq'::regclass),
    code character varying(20) NOT NULL,
    label character varying(100) NOT NULL,
    description text,
    color character varying(50),
    sort_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL,
    CONSTRAINT case_types_pkey PRIMARY KEY (type_id),
    CONSTRAINT uk_case_types_code UNIQUE (code)
);

CREATE TABLE IF NOT EXISTS public.project_codes (
    code_id integer NOT NULL DEFAULT nextval('public.project_codes_code_id_seq'::regclass),
    code character varying(10) NOT NULL,
    label character varying(200) NOT NULL,
    description text,
    sort_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT (now() AT TIME ZONE 'Asia/Taipei') NOT NULL,
    updated_at timestamp without time zone DEFAULT (now() AT TIME ZONE 'Asia/Taipei') NOT NULL,
    CONSTRAINT project_codes_pkey PRIMARY KEY (code_id),
    CONSTRAINT project_codes_code_key UNIQUE (code)
);

CREATE TABLE IF NOT EXISTS public.case_type_projects (
    id integer NOT NULL DEFAULT nextval('public.case_type_projects_id_seq'::regclass),
    type_id integer NOT NULL,
    project_id integer NOT NULL,
    CONSTRAINT case_type_projects_pkey PRIMARY KEY (id),
    CONSTRAINT uk_ctype_project UNIQUE (type_id, project_id)
);

CREATE TABLE IF NOT EXISTS public.problem_category_case_types (
    id integer NOT NULL DEFAULT nextval('public.problem_category_case_types_id_seq'::regclass),
    category_id integer NOT NULL,
    type_id integer NOT NULL,
    CONSTRAINT problem_category_case_types_pkey PRIMARY KEY (id),
    CONSTRAINT uk_cat_ctype UNIQUE (category_id, type_id)
);

CREATE TABLE IF NOT EXISTS public.problem_category_projects (
    id integer NOT NULL DEFAULT nextval('public.problem_category_projects_id_seq'::regclass),
    category_id integer NOT NULL,
    project_id integer NOT NULL,
    CONSTRAINT problem_category_projects_pkey PRIMARY KEY (id),
    CONSTRAINT uk_cat_project UNIQUE (category_id, project_id)
);

-- 4) Sequence ownership/defaults
ALTER SEQUENCE public.case_types_type_id_seq OWNED BY public.case_types.type_id;
ALTER SEQUENCE public.case_type_projects_id_seq OWNED BY public.case_type_projects.id;
ALTER SEQUENCE public.problem_category_case_types_id_seq OWNED BY public.problem_category_case_types.id;
ALTER SEQUENCE public.problem_category_projects_id_seq OWNED BY public.problem_category_projects.id;
ALTER SEQUENCE public.project_codes_code_id_seq OWNED BY public.project_codes.code_id;

ALTER TABLE ONLY public.case_types
    ALTER COLUMN type_id SET DEFAULT nextval('public.case_types_type_id_seq'::regclass);

ALTER TABLE ONLY public.case_type_projects
    ALTER COLUMN id SET DEFAULT nextval('public.case_type_projects_id_seq'::regclass);

ALTER TABLE ONLY public.problem_category_case_types
    ALTER COLUMN id SET DEFAULT nextval('public.problem_category_case_types_id_seq'::regclass);

ALTER TABLE ONLY public.problem_category_projects
    ALTER COLUMN id SET DEFAULT nextval('public.problem_category_projects_id_seq'::regclass);

ALTER TABLE ONLY public.project_codes
    ALTER COLUMN code_id SET DEFAULT nextval('public.project_codes_code_id_seq'::regclass);

-- 5) Indexes
CREATE INDEX IF NOT EXISTS idx_cat_ctype_category
    ON public.problem_category_case_types USING btree (category_id);

CREATE INDEX IF NOT EXISTS idx_cat_ctype_type
    ON public.problem_category_case_types USING btree (type_id);

CREATE INDEX IF NOT EXISTS idx_cat_proj_category
    ON public.problem_category_projects USING btree (category_id);

CREATE INDEX IF NOT EXISTS idx_cat_proj_project
    ON public.problem_category_projects USING btree (project_id);

CREATE INDEX IF NOT EXISTS idx_ctype_proj_project
    ON public.case_type_projects USING btree (project_id);

CREATE INDEX IF NOT EXISTS idx_ctype_proj_type
    ON public.case_type_projects USING btree (type_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_google_sub
    ON public.users USING btree (google_sub)
    WHERE google_sub IS NOT NULL;

-- 6) Foreign keys
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'case_logs_ref_case_id_fkey') THEN
        ALTER TABLE ONLY public.case_logs
            ADD CONSTRAINT case_logs_ref_case_id_fkey
            FOREIGN KEY (ref_case_id) REFERENCES public.cases(case_id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'case_type_projects_project_id_fkey') THEN
        ALTER TABLE ONLY public.case_type_projects
            ADD CONSTRAINT case_type_projects_project_id_fkey
            FOREIGN KEY (project_id) REFERENCES public.projects(project_id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'case_type_projects_type_id_fkey') THEN
        ALTER TABLE ONLY public.case_type_projects
            ADD CONSTRAINT case_type_projects_type_id_fkey
            FOREIGN KEY (type_id) REFERENCES public.case_types(type_id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'problem_categories_project_id_fkey') THEN
        ALTER TABLE ONLY public.problem_categories
            ADD CONSTRAINT problem_categories_project_id_fkey
            FOREIGN KEY (project_id) REFERENCES public.projects(project_id) ON DELETE SET NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'problem_category_case_types_category_id_fkey') THEN
        ALTER TABLE ONLY public.problem_category_case_types
            ADD CONSTRAINT problem_category_case_types_category_id_fkey
            FOREIGN KEY (category_id) REFERENCES public.problem_categories(category_id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'problem_category_case_types_type_id_fkey') THEN
        ALTER TABLE ONLY public.problem_category_case_types
            ADD CONSTRAINT problem_category_case_types_type_id_fkey
            FOREIGN KEY (type_id) REFERENCES public.case_types(type_id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'problem_category_projects_category_id_fkey') THEN
        ALTER TABLE ONLY public.problem_category_projects
            ADD CONSTRAINT problem_category_projects_category_id_fkey
            FOREIGN KEY (category_id) REFERENCES public.problem_categories(category_id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'problem_category_projects_project_id_fkey') THEN
        ALTER TABLE ONLY public.problem_category_projects
            ADD CONSTRAINT problem_category_projects_project_id_fkey
            FOREIGN KEY (project_id) REFERENCES public.projects(project_id) ON DELETE CASCADE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'projects_project_code_id_fkey') THEN
        ALTER TABLE ONLY public.projects
            ADD CONSTRAINT projects_project_code_id_fkey
            FOREIGN KEY (project_code_id) REFERENCES public.project_codes(code_id) ON DELETE SET NULL;
    END IF;
END $$;
