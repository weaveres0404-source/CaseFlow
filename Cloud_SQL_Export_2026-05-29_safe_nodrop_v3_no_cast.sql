--
-- PostgreSQL database dump
--



-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

-- *not* creating schema, since initdb creates it


--
-- Name: update_updated_at(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE OR REPLACE FUNCTION public.update_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: customers; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.customers (
    customer_id integer CONSTRAINT "Customers_customer_id_not_null" NOT NULL,
    customer_name character varying(100) CONSTRAINT "Customers_customer_name_not_null" NOT NULL,
    contact_person character varying(100),
    contact_phone character varying(30),
    contact_email character varying(150),
    address character varying(255),
    remarks text,
    is_active boolean DEFAULT true CONSTRAINT "Customers_is_active_not_null" NOT NULL,
    created_at timestamp with time zone DEFAULT now() CONSTRAINT "Customers_created_at_not_null" NOT NULL,
    updated_at timestamp with time zone DEFAULT now() CONSTRAINT "Customers_updated_at_not_null" NOT NULL
);


--
-- Name: Customers_customer_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."Customers_customer_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Customers_customer_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."Customers_customer_id_seq" OWNED BY public.customers.customer_id;


--
-- Name: DataProtectionKeys; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."DataProtectionKeys" (
    "Id" integer NOT NULL,
    "FriendlyName" text,
    "Xml" text
);


--
-- Name: DataProtectionKeys_Id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."DataProtectionKeys_Id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: DataProtectionKeys_Id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."DataProtectionKeys_Id_seq" OWNED BY public."DataProtectionKeys"."Id";


--
-- Name: users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.users (
    user_id integer CONSTRAINT "Users_userid_not_null" NOT NULL,
    username character varying(50) CONSTRAINT "Users_username_not_null" NOT NULL,
    password_hash character varying(255) CONSTRAINT "Users_password_hash_not_null" NOT NULL,
    full_name character varying(100) CONSTRAINT "Users_full_name_not_null" NOT NULL,
    email character varying(150) CONSTRAINT "Users_email_not_null" NOT NULL,
    phone character varying(30),
    role character varying(20) DEFAULT '' CONSTRAINT "Users_role_not_null" NOT NULL,
    is_active boolean DEFAULT true CONSTRAINT "Users_is_active_not_null" NOT NULL,
    last_login_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() CONSTRAINT "Users_created_at_not_null" NOT NULL,
    updated_at timestamp with time zone DEFAULT now() CONSTRAINT "Users_updated_at_not_null" NOT NULL,
    must_change_password boolean DEFAULT false NOT NULL,
    google_sub character varying(64),
    google_email character varying(255),
    auth_provider character varying(20) DEFAULT '' NOT NULL,
    CONSTRAINT check_role_types CHECK ((role = ANY (ARRAY[('')::text, ('')::text, ('')::text])))
);


--
-- Name: Users_userid_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public."Users_userid_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Users_userid_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public."Users_userid_seq" OWNED BY public.users.user_id;


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: attachments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.attachments (
    attachment_id integer NOT NULL,
    file_name character varying(255) NOT NULL,
    stored_name character varying(255) NOT NULL,
    file_path character varying(500) NOT NULL,
    file_size integer DEFAULT 0 NOT NULL,
    mime_type character varying(100) NOT NULL,
    entity_type character varying(50) NOT NULL,
    entity_id integer NOT NULL,
    uploaded_by integer NOT NULL,
    uploaded_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT attachments_entity_type_check CHECK ((entity_type = ANY (ARRAY[('')::text, ('')::text, ('')::text, ('')::text])))
);


--
-- Name: attachments_attachment_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.attachments ALTER COLUMN attachment_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.attachments_attachment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: audit_logs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.audit_logs (
    audit_id bigint NOT NULL,
    user_id integer NOT NULL,
    case_id integer,
    action character varying(100) NOT NULL,
    entity_type character varying(50),
    entity_id integer,
    old_value jsonb,
    new_value jsonb,
    ip_address character varying(45),
    user_agent character varying(500),
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: audit_logs_audit_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.audit_logs ALTER COLUMN audit_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.audit_logs_audit_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: case_assignments; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.case_assignments (
    assignment_id integer NOT NULL,
    case_id integer NOT NULL,
    se_user_id integer NOT NULL,
    assigned_by integer NOT NULL,
    is_primary boolean DEFAULT false NOT NULL,
    instructions text,
    expected_completion_date date,
    is_active boolean DEFAULT true NOT NULL,
    assigned_at timestamp with time zone DEFAULT now() NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: case_assignments_assignment_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.case_assignments ALTER COLUMN assignment_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.case_assignments_assignment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: case_estimations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.case_estimations (
    estimation_id integer NOT NULL,
    case_id integer NOT NULL,
    estimator_user_id integer NOT NULL,
    seq_no integer NOT NULL,
    request_date date NOT NULL,
    summary text NOT NULL,
    estimated_hours numeric(6,2) DEFAULT 0.00 NOT NULL,
    reply_date date,
    estimation_status smallint DEFAULT 10 NOT NULL,
    remarks text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    case_log_id integer
);


--
-- Name: case_estimations_estimation_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.case_estimations ALTER COLUMN estimation_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.case_estimations_estimation_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: case_logs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.case_logs (
    log_id integer NOT NULL,
    case_id integer NOT NULL,
    handler_user_id integer NOT NULL,
    log_date date DEFAULT CURRENT_DATE NOT NULL,
    handling_method text NOT NULL,
    handling_result text,
    hours_spent numeric(6,2) DEFAULT 0.00 NOT NULL,
    headcount smallint DEFAULT 1 NOT NULL,
    status_after smallint DEFAULT 30 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    eval_hours numeric(6,2),
    ref_case_id integer
);


--
-- Name: case_logs_log_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.case_logs ALTER COLUMN log_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.case_logs_log_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: case_replies; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.case_replies (
    reply_id integer NOT NULL,
    case_id integer NOT NULL,
    replier_user_id integer NOT NULL,
    reply_date date DEFAULT CURRENT_DATE NOT NULL,
    reply_content text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: case_replies_reply_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.case_replies ALTER COLUMN reply_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.case_replies_reply_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: case_type_projects; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.case_type_projects (
    id integer NOT NULL,
    type_id integer NOT NULL,
    project_id integer NOT NULL
);


--
-- Name: case_type_projects_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.case_type_projects_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: case_type_projects_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.case_type_projects_id_seq OWNED BY public.case_type_projects.id;


--
-- Name: case_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.case_types (
    type_id integer NOT NULL,
    code character varying(20) NOT NULL,
    label character varying(100) NOT NULL,
    description text,
    color character varying(50),
    sort_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);


--
-- Name: case_types_type_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.case_types_type_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: case_types_type_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.case_types_type_id_seq OWNED BY public.case_types.type_id;


--
-- Name: cases; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.cases (
    case_id integer NOT NULL,
    case_number character varying(30) NOT NULL,
    project_id integer NOT NULL,
    customer_id integer NOT NULL,
    category_id integer NOT NULL,
    module_id integer,
    reporter_name character varying(100) NOT NULL,
    reporter_phone character varying(30),
    reporter_email character varying(150),
    case_type character varying(20) DEFAULT '' NOT NULL,
    priority character varying(10) DEFAULT '' NOT NULL,
    description text NOT NULL,
    status smallint DEFAULT 10 NOT NULL,
    created_by integer NOT NULL,
    assigned_pm_id integer,
    closed_by integer,
    cancelled_by integer,
    related_case_id integer,
    relation_type character varying(20),
    closed_at timestamp with time zone,
    cancelled_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    due_at timestamp with time zone,
    total_hours numeric(7,2) DEFAULT 0 NOT NULL,
    CONSTRAINT cases_case_type_check CHECK ((case_type = ANY (ARRAY[('')::text, ('')::text, ('')::text, ('')::text, ('')::text]))),
    CONSTRAINT cases_priority_check CHECK ((priority = ANY (ARRAY[('')::text, ('')::text, ('')::text]))),
    CONSTRAINT cases_relation_type_check CHECK ((relation_type = ANY (ARRAY[('')::text, ('')::text]))),
    CONSTRAINT check_case_status CHECK ((status = ANY (ARRAY[10, 20, 30, 35, 40, 50, 60])))
);


--
-- Name: COLUMN cases.due_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.cases.due_at IS '???芣迫??';


--
-- Name: COLUMN cases.total_hours; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.cases.total_hours IS '蝝航??撌交?嚗???嚗 API ?潭?甈⊥憓?靽格 CaseLog ????;


--
-- Name: cases_case_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.cases ALTER COLUMN case_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.cases_case_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: project_members; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.project_members (
    member_id integer CONSTRAINT member_member_id_not_null NOT NULL,
    project_id integer CONSTRAINT member_project_id_not_null NOT NULL,
    user_id integer CONSTRAINT member_user_id_not_null NOT NULL,
    member_role character varying DEFAULT '' CONSTRAINT member_member_role_not_null NOT NULL,
    joined_at date DEFAULT CURRENT_DATE CONSTRAINT member_joined_at_not_null NOT NULL,
    is_active boolean DEFAULT true CONSTRAINT member_is_active_not_null NOT NULL,
    created_at timestamp with time zone DEFAULT now() CONSTRAINT member_created_at_not_null NOT NULL
);


--
-- Name: member_member_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.member_member_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: member_member_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.member_member_id_seq OWNED BY public.project_members.member_id;


--
-- Name: notifications; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notifications (
    notification_id integer NOT NULL,
    recipient_user_id integer NOT NULL,
    case_id integer,
    notification_type character varying(50) NOT NULL,
    title character varying(200) NOT NULL,
    message text NOT NULL,
    is_read boolean DEFAULT false NOT NULL,
    read_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT notifications_notification_type_check CHECK ((notification_type = ANY (ARRAY[('')::text, ('')::text, ('')::text, ('')::text, ('')::text, ('')::text, ('')::text, ('')::text])))
);


--
-- Name: notifications_notification_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.notifications ALTER COLUMN notification_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.notifications_notification_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: problem_categories; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.problem_categories (
    category_id integer NOT NULL,
    category_name character varying(100) NOT NULL,
    description character varying(255),
    sort_order integer NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    parent_type character varying(50),
    case_type_filter character varying(20),
    project_id integer
);


--
-- Name: COLUMN problem_categories.case_type_filter; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.problem_categories.case_type_filter IS '撠?獢辣憿?嚗EPAIR/EVALUATION/MAINTENANCE/UHD嚗?NULL 銵函內??????舫';


--
-- Name: problem_categories_category_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.problem_categories_category_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: problem_categories_category_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.problem_categories_category_id_seq OWNED BY public.problem_categories.category_id;


--
-- Name: problem_category_case_types; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.problem_category_case_types (
    id integer NOT NULL,
    category_id integer NOT NULL,
    type_id integer NOT NULL
);


--
-- Name: problem_category_case_types_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.problem_category_case_types_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: problem_category_case_types_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.problem_category_case_types_id_seq OWNED BY public.problem_category_case_types.id;


--
-- Name: problem_category_projects; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.problem_category_projects (
    id integer NOT NULL,
    category_id integer NOT NULL,
    project_id integer NOT NULL
);


--
-- Name: problem_category_projects_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.problem_category_projects_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: problem_category_projects_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.problem_category_projects_id_seq OWNED BY public.problem_category_projects.id;


--
-- Name: project_codes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.project_codes (
    code_id integer NOT NULL,
    code character varying(10) NOT NULL,
    label character varying(200) NOT NULL,
    description text,
    sort_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT (now() AT TIME ZONE '') NOT NULL,
    updated_at timestamp without time zone DEFAULT (now() AT TIME ZONE '') NOT NULL
);


--
-- Name: project_codes_code_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.project_codes_code_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: project_codes_code_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.project_codes_code_id_seq OWNED BY public.project_codes.code_id;


--
-- Name: projects; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.projects (
    project_id integer CONSTRAINT project_project_id_not_null NOT NULL,
    project_code character varying(20) CONSTRAINT project_project_code_not_null NOT NULL,
    project_name character varying(100) CONSTRAINT project_project_name_not_null NOT NULL,
    customer_id integer CONSTRAINT project_customer_id_not_null NOT NULL,
    description text,
    start_date date,
    end_date date,
    is_active boolean DEFAULT true CONSTRAINT project_is_active_not_null NOT NULL,
    created_at timestamp with time zone DEFAULT now() CONSTRAINT project_created_at_not_null NOT NULL,
    updated_at timestamp with time zone DEFAULT now() CONSTRAINT project_updated_at_not_null NOT NULL,
    allowed_case_types text,
    project_code_id integer
);


--
-- Name: project_project_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.project_project_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: project_project_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.project_project_id_seq OWNED BY public.projects.project_id;


--
-- Name: schema_migrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.schema_migrations (
    version integer NOT NULL,
    description character varying(255),
    installed_on timestamp without time zone DEFAULT now()
);


--
-- Name: system_modules; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.system_modules (
    module_id integer NOT NULL,
    project_id integer NOT NULL,
    module_name character varying(100) NOT NULL,
    description character varying(255),
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: system_modules_module_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.system_modules_module_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: system_modules_module_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.system_modules_module_id_seq OWNED BY public.system_modules.module_id;


--
-- Name: DataProtectionKeys Id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DataProtectionKeys" ALTER COLUMN "Id" SET DEFAULT nextval('public."DataProtectionKeys_Id_seq"'::regclass);


--
-- Name: case_type_projects id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_type_projects ALTER COLUMN id SET DEFAULT nextval('public.case_type_projects_id_seq'::regclass);


--
-- Name: case_types type_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_types ALTER COLUMN type_id SET DEFAULT nextval('public.case_types_type_id_seq'::regclass);


--
-- Name: customers customer_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customers ALTER COLUMN customer_id SET DEFAULT nextval('public."Customers_customer_id_seq"'::regclass);


--
-- Name: problem_categories category_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_categories ALTER COLUMN category_id SET DEFAULT nextval('public.problem_categories_category_id_seq'::regclass);


--
-- Name: problem_category_case_types id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_case_types ALTER COLUMN id SET DEFAULT nextval('public.problem_category_case_types_id_seq'::regclass);


--
-- Name: problem_category_projects id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_projects ALTER COLUMN id SET DEFAULT nextval('public.problem_category_projects_id_seq'::regclass);


--
-- Name: project_codes code_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.project_codes ALTER COLUMN code_id SET DEFAULT nextval('public.project_codes_code_id_seq'::regclass);


--
-- Name: project_members member_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.project_members ALTER COLUMN member_id SET DEFAULT nextval('public.member_member_id_seq'::regclass);


--
-- Name: projects project_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.projects ALTER COLUMN project_id SET DEFAULT nextval('public.project_project_id_seq'::regclass);


--
-- Name: system_modules module_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.system_modules ALTER COLUMN module_id SET DEFAULT nextval('public.system_modules_module_id_seq'::regclass);


--
-- Name: users user_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users ALTER COLUMN user_id SET DEFAULT nextval('public."Users_userid_seq"'::regclass);


--
-- Data for Name: DataProtectionKeys; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."DataProtectionKeys" ("Id", "FriendlyName", "Xml") FROM stdin;
1	key-7293111c-40bb-4a18-a5d0-125dd106992e	<key id="7293111c-40bb-4a18-a5d0-125dd106992e" version="1"><creationDate>2026-05-26T11:05:25.8795477Z</creationDate><activationDate>2026-05-26T11:05:25.8795477Z</activationDate><expirationDate>2026-08-24T11:05:25.8795477Z</expirationDate><descriptor deserializerType="Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60"><descriptor><encryption algorithm="AES_256_CBC" /><validation algorithm="HMACSHA256" /><masterKey p4:requiresEncryption="true" xmlns:p4="http://schemas.asp.net/2015/03/dataProtection"><!-- Warning: the key below is in an unencrypted form. --><value>cSQNlcREhGPzyE8rSaPW3z0qDn+JBMBngX6rSPTNmfFLO2IDDCTZtVAJBwmVP8JfrseZesWNRyeB0D17wCvbMQ==</value></masterKey></descriptor></descriptor></key>
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
\.


--
-- Data for Name: attachments; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.attachments (attachment_id, file_name, stored_name, file_path, file_size, mime_type, entity_type, entity_id, uploaded_by, uploaded_at) FROM stdin;
1	msxjprau.jpg	d9a33a0e-2ea3-4cf3-9dc3-3b157da1d581.jpg	https://storage.googleapis.com/caseflow-test-files/d9a33a0e-2ea3-4cf3-9dc3-3b157da1d581.jpg	71300	image/jpeg	case	36	2	2026-05-26 01:52:49.848807+08
2	images.jpg	c5163814-a881-453f-a4e8-f40c5c7e23b2.jpg	https://storage.googleapis.com/caseflow-test-files/c5163814-a881-453f-a4e8-f40c5c7e23b2.jpg	8381	image/jpeg	case	37	2	2026-05-25 18:15:37.49601+08
3	SldConfigs.xml	88713746-b71e-4cad-8613-8c0489f78ae8.xml	https://storage.googleapis.com/caseflow-test-files/88713746-b71e-4cad-8613-8c0489f78ae8.xml	278	text/xml	case	51	2	2026-05-28 10:12:15.817628+08
4	gmail.png	478956cb-3be6-43d3-8986-0e63ba39d48d.png	https://storage.googleapis.com/caseflow-test-files/478956cb-3be6-43d3-8986-0e63ba39d48d.png	10991	image/png	case	51	2	2026-05-28 10:12:15.996006+08
5	gmail (1).png	fb91f5fa-388d-4215-bffc-228c4fb7e103.png	https://storage.googleapis.com/caseflow-test-files/fb91f5fa-388d-4215-bffc-228c4fb7e103.png	10991	image/png	case_log	25	8	2026-05-28 13:47:25.376954+08
6	gmail (1).png	e530b2a4-4237-42d9-aa7e-66480abf8e5e.png	https://storage.googleapis.com/caseflow-test-files/e530b2a4-4237-42d9-aa7e-66480abf8e5e.png	10991	image/png	case	64	2	2026-05-28 16:03:59.71527+08
7	SldConfigs (1).xml	d13d1695-67c6-4c01-b70a-842865959bf5.xml	https://storage.googleapis.com/caseflow-test-files/d13d1695-67c6-4c01-b70a-842865959bf5.xml	278	text/xml	case	64	2	2026-05-28 16:03:59.881025+08
\.


--
-- Data for Name: audit_logs; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.audit_logs (audit_id, user_id, case_id, action, entity_type, entity_id, old_value, new_value, ip_address, user_agent, created_at) FROM stdin;
1	2	8	CASE_CREATED	case	8	\N	{"status": 10}	127.0.0.1	demo/seed	2026-03-25 16:45:00+08
2	2	8	CASE_ASSIGNED	case	8	{"status": 10}	{"status": 20, "assigned_se_user_id": 11}	127.0.0.1	demo/seed	2026-03-26 18:45:00+08
3	11	8	WORK_COMPLETED	case	8	{"status": 30}	{"status": 40}	127.0.0.1	demo/seed	2026-03-30 22:45:00+08
4	2	8	CASE_CLOSED	case	8	{"status": 40}	{"status": 50}	127.0.0.1	demo/seed	2026-04-01 16:45:00+08
5	3	9	CASE_CREATED	case	9	\N	{"status": 10}	127.0.0.1	demo/seed	2026-03-30 13:00:00+08
6	3	9	CASE_ASSIGNED	case	9	{"status": 10}	{"status": 20, "assigned_se_user_id": 11}	127.0.0.1	demo/seed	2026-03-31 15:00:00+08
7	11	9	WORK_COMPLETED	case	9	{"status": 30}	{"status": 40}	127.0.0.1	demo/seed	2026-04-04 19:00:00+08
8	3	9	CASE_CLOSED	case	9	{"status": 40}	{"status": 50}	127.0.0.1	demo/seed	2026-04-06 13:00:00+08
9	2	10	CASE_CREATED	case	10	\N	{"status": 10}	127.0.0.1	demo/seed	2026-04-05 14:30:00+08
10	2	10	CASE_ASSIGNED	case	10	{"status": 10}	{"status": 20, "assigned_se_user_id": 7}	127.0.0.1	demo/seed	2026-04-06 16:30:00+08
11	7	10	WORK_COMPLETED	case	10	{"status": 30}	{"status": 40}	127.0.0.1	demo/seed	2026-04-10 20:30:00+08
12	2	10	CASE_CLOSED	case	10	{"status": 40}	{"status": 50}	127.0.0.1	demo/seed	2026-04-12 14:30:00+08
13	3	11	CASE_CREATED	case	11	\N	{"status": 10}	127.0.0.1	demo/seed	2026-04-15 17:00:00+08
14	3	11	CASE_ASSIGNED	case	11	{"status": 10}	{"status": 20, "assigned_se_user_id": 11}	127.0.0.1	demo/seed	2026-04-16 19:00:00+08
15	11	11	WORK_COMPLETED	case	11	{"status": 30}	{"status": 40}	127.0.0.1	demo/seed	2026-04-20 23:00:00+08
16	3	12	CASE_CREATED	case	12	\N	{"status": 10}	127.0.0.1	demo/seed	2026-04-18 11:20:00+08
17	3	12	CASE_ASSIGNED	case	12	{"status": 10}	{"status": 20, "assigned_se_user_id": 11}	127.0.0.1	demo/seed	2026-04-19 13:20:00+08
18	11	12	WORK_COMPLETED	case	12	{"status": 30}	{"status": 40}	127.0.0.1	demo/seed	2026-04-23 17:20:00+08
19	2	13	CASE_CREATED	case	13	\N	{"status": 10}	127.0.0.1	demo/seed	2026-04-20 15:30:00+08
20	2	13	CASE_CANCELLED	case	13	{"status": 10}	{"reason": "摰Ｘ靘?芾?閫?捱嚗?桀?瘨?, "status": 60}	127.0.0.1	demo/seed	2026-04-20 19:30:00+08
21	3	14	CASE_CREATED	case	14	\N	{"status": 10}	127.0.0.1	demo/seed	2026-04-22 10:15:00+08
22	3	14	CASE_ASSIGNED	case	14	{"status": 10}	{"status": 20, "assigned_se_user_id": 7}	127.0.0.1	demo/seed	2026-04-23 12:15:00+08
23	3	15	CASE_CREATED	case	15	\N	{"status": 10}	127.0.0.1	demo/seed	2026-04-28 10:00:00+08
24	3	15	CASE_ASSIGNED	case	15	{"status": 10}	{"status": 20, "assigned_se_user_id": 7}	127.0.0.1	demo/seed	2026-04-29 12:00:00+08
25	2	16	CASE_CREATED	case	16	\N	{"status": 10}	127.0.0.1	demo/seed	2026-05-07 09:30:00+08
26	2	16	CASE_ASSIGNED	case	16	{"status": 10}	{"status": 20, "assigned_se_user_id": 7}	127.0.0.1	demo/seed	2026-05-08 11:30:00+08
27	2	17	CASE_CREATED	case	17	\N	{"status": 10}	127.0.0.1	demo/seed	2026-05-11 08:30:00+08
28	1	17	LOG_CREATED	case_log	17	\N	\N	\N	\N	2026-05-12 20:25:31.423551+08
29	1	16	LOG_CREATED	case_log	16	\N	\N	\N	\N	2026-05-12 20:26:32.305711+08
30	1	16	CASE_ASSIGNED	case	16	\N	\N	\N	\N	2026-05-12 21:42:02.049491+08
31	2	6	CASE_ASSIGNED	case	6	\N	\N	\N	\N	2026-05-12 21:58:15.688577+08
32	2	6	CASE_ASSIGNED	case	6	\N	\N	\N	\N	2026-05-12 21:58:19.633772+08
33	5	29	LOG_CREATED	case_log	29	\N	\N	\N	\N	2026-05-12 22:43:41.280158+08
34	5	29	LOG_CREATED	case_log	29	\N	\N	\N	\N	2026-05-12 22:43:52.266122+08
35	1	16	LOG_CREATED	case_log	16	\N	\N	\N	\N	2026-05-12 22:44:23.30552+08
36	1	16	LOG_CREATED	case_log	16	\N	\N	\N	\N	2026-05-12 22:44:41.628378+08
37	1	16	LOG_CREATED	case_log	16	\N	\N	\N	\N	2026-05-12 22:49:17.49878+08
38	1	16	LOG_CREATED	case_log	16	\N	\N	\N	\N	2026-05-12 23:16:42.53118+08
39	1	16	LOG_CREATED	case_log	16	\N	\N	\N	\N	2026-05-12 23:16:54.211554+08
40	2	36	CASE_CREATED	case	36	\N	\N	\N	\N	2026-05-25 17:52:47.744811+08
41	2	36	CASE_CANCELLED	case	36	\N	\N	\N	\N	2026-05-25 10:14:47.773196+08
42	2	37	CASE_CREATED	case	37	\N	\N	\N	\N	2026-05-25 10:15:36.901076+08
43	2	38	CASE_CREATED	case	38	\N	\N	\N	\N	2026-05-26 03:08:30.269531+08
44	2	39	CASE_CREATED	case	39	\N	\N	\N	\N	2026-05-27 05:56:04.898669+08
45	2	40	CASE_CREATED	case	40	\N	\N	\N	\N	2026-05-27 05:57:39.175777+08
46	2	41	CASE_CREATED	case	41	\N	\N	\N	\N	2026-05-27 05:58:19.113291+08
47	2	42	CASE_CREATED	case	42	\N	\N	\N	\N	2026-05-27 05:59:08.695516+08
48	1	43	CASE_CREATED	case	43	\N	\N	\N	\N	2026-05-27 06:16:02.360276+08
49	2	44	CASE_CREATED	case	44	\N	\N	\N	\N	2026-05-27 06:28:12.286087+08
50	1	45	CASE_CREATED	case	45	\N	\N	\N	\N	2026-05-27 06:59:22.723017+08
51	1	46	CASE_CREATED	case	46	\N	\N	\N	\N	2026-05-27 07:17:59.328835+08
52	1	47	CASE_CREATED	case	47	\N	\N	\N	\N	2026-05-27 15:20:01.887998+08
53	1	48	CASE_CREATED	case	48	\N	\N	\N	\N	2026-05-27 15:23:51.73284+08
54	1	49	CASE_CREATED	case	49	\N	\N	\N	\N	2026-05-27 16:24:07.094805+08
55	1	50	CASE_CREATED	case	50	\N	\N	\N	\N	2026-05-27 18:04:22.547691+08
56	2	51	CASE_CREATED	case	51	\N	\N	\N	\N	2026-05-28 10:12:14.716769+08
57	3	52	CASE_CREATED	case	52	\N	\N	\N	\N	2026-05-28 11:34:50.575028+08
58	3	52	CASE_ASSIGNED	case	52	\N	\N	\N	\N	2026-05-28 11:34:51.607421+08
59	3	52	LOG_CREATED	case_log	52	\N	\N	\N	\N	2026-05-28 11:34:51.742295+08
60	3	52	CASE_CANCELLED	case	52	\N	\N	\N	\N	2026-05-28 11:34:51.901146+08
61	2	51	CASE_ASSIGNED	case	51	\N	\N	\N	\N	2026-05-28 11:46:49.203657+08
62	2	51	CASE_ASSIGNED	case	51	\N	\N	\N	\N	2026-05-28 13:39:40.040359+08
63	7	51	LOG_CREATED	case_log	51	\N	\N	\N	\N	2026-05-28 13:46:03.982049+08
64	7	51	LOG_CREATED	case_log	51	\N	\N	\N	\N	2026-05-28 13:46:27.087256+08
65	8	51	LOG_CREATED	case_log	51	\N	\N	\N	\N	2026-05-28 13:47:24.951867+08
66	1	45	CASE_ASSIGNED	case	45	\N	\N	\N	\N	2026-05-28 13:50:31.323511+08
67	12	45	LOG_CREATED	case_log	45	\N	\N	\N	\N	2026-05-28 13:51:12.872403+08
68	2	53	CASE_CREATED	case	53	\N	\N	\N	\N	2026-05-28 13:55:33.521144+08
69	2	53	CASE_ASSIGNED	case	53	\N	\N	\N	\N	2026-05-28 13:55:38.138597+08
70	5	53	LOG_CREATED	case_log	53	\N	\N	\N	\N	2026-05-28 14:46:44.422941+08
71	5	53	WORK_COMPLETED	case	53	\N	\N	\N	\N	2026-05-28 14:46:50.825526+08
72	5	53	CASE_CLOSED	case	53	\N	\N	\N	\N	2026-05-28 14:47:12.383102+08
73	5	6	LOG_CREATED	case_log	6	\N	\N	\N	\N	2026-05-28 15:17:14.125293+08
74	2	64	CASE_CREATED	case	64	\N	\N	\N	\N	2026-05-28 16:03:58.547559+08
75	2	64	CASE_ASSIGNED	case	64	\N	\N	\N	\N	2026-05-28 16:06:32.674395+08
76	2	64	CASE_ASSIGNED	case	64	\N	\N	\N	\N	2026-05-28 16:09:49.990557+08
77	2	64	LOG_CREATED	case_log	64	\N	\N	\N	\N	2026-05-28 17:00:39.748603+08
78	5	64	LOG_CREATED	case_log	64	\N	\N	\N	\N	2026-05-28 17:00:55.715863+08
79	5	64	LOG_CREATED	case_log	64	\N	\N	\N	\N	2026-05-28 17:01:16.367773+08
80	5	64	WORK_COMPLETED	case	64	\N	\N	\N	\N	2026-05-28 17:01:23.447487+08
81	2	64	CASE_CLOSED	case	64	\N	\N	\N	\N	2026-05-28 17:01:35.372191+08
82	2	63	CASE_ASSIGNED	case	63	\N	\N	\N	\N	2026-05-28 17:02:06.607188+08
83	5	63	LOG_CREATED	case_log	63	\N	\N	\N	\N	2026-05-28 17:02:48.15753+08
84	5	63	WORK_COMPLETED	case	63	\N	\N	\N	\N	2026-05-28 17:02:57.930178+08
85	2	63	CASE_CLOSED	case	63	\N	\N	\N	\N	2026-05-28 17:09:25.83175+08
86	2	65	CASE_CREATED	case	65	\N	\N	\N	\N	2026-05-28 17:13:00.112375+08
87	2	65	CASE_ASSIGNED	case	65	\N	\N	\N	\N	2026-05-28 17:13:13.522226+08
88	2	66	CASE_CREATED	case	66	\N	\N	\N	\N	2026-05-28 17:18:50.085056+08
89	2	66	CASE_ASSIGNED	case	66	\N	\N	\N	\N	2026-05-28 17:18:55.372692+08
90	5	66	LOG_CREATED	case_log	66	\N	\N	\N	\N	2026-05-28 17:19:55.942465+08
91	5	66	WORK_COMPLETED	case	66	\N	\N	\N	\N	2026-05-28 17:20:00.523289+08
92	2	65	LOG_CREATED	case_log	65	\N	\N	\N	\N	2026-05-28 17:21:15.330513+08
93	2	67	CASE_CREATED	case	67	\N	\N	\N	\N	2026-05-28 20:00:21.924478+08
94	2	67	CASE_ASSIGNED	case	67	\N	\N	\N	\N	2026-05-28 20:00:34.67457+08
95	2	67	CASE_ASSIGNED	case	67	\N	\N	\N	\N	2026-05-28 20:00:38.551197+08
96	2	67	LOG_CREATED	case_log	67	\N	\N	\N	\N	2026-05-28 20:10:35.407401+08
97	2	67	WORK_COMPLETED	case	67	\N	\N	\N	\N	2026-05-28 20:10:42.993085+08
98	2	67	CASE_CLOSED	case	67	\N	\N	\N	\N	2026-05-28 20:10:46.767162+08
99	2	68	CASE_CREATED	case	68	\N	\N	\N	\N	2026-05-28 20:15:33.568324+08
100	2	69	CASE_CREATED	case	69	\N	\N	\N	\N	2026-05-28 20:27:56.718532+08
101	2	69	CASE_ASSIGNED	case	69	\N	\N	\N	\N	2026-05-28 20:28:00.447741+08
\.


--
-- Data for Name: case_assignments; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.case_assignments (assignment_id, case_id, se_user_id, assigned_by, is_primary, instructions, expected_completion_date, is_active, assigned_at, created_at) FROM stdin;
1	8	11	2	t	隢????銝行?拚勗???脣漲??2026-04-09	t	2026-03-26 18:45:00+08	2026-03-26 18:45:00+08
2	9	11	3	t	隢????銝行?拚勗???脣漲??2026-04-14	t	2026-03-31 15:00:00+08	2026-03-31 15:00:00+08
3	10	7	2	t	隢????銝行?拚勗???脣漲??2026-04-20	t	2026-04-06 16:30:00+08	2026-04-06 16:30:00+08
4	11	11	3	t	隢????銝行?拚勗???脣漲??2026-04-30	t	2026-04-16 19:00:00+08	2026-04-16 19:00:00+08
5	12	11	3	t	隢????銝行?拚勗???脣漲??2026-05-03	t	2026-04-19 13:20:00+08	2026-04-19 13:20:00+08
6	14	7	3	t	隢????銝行?拚勗???脣漲??2026-05-07	t	2026-04-23 12:15:00+08	2026-04-23 12:15:00+08
7	15	7	3	t	隢????銝行?拚勗???脣漲??2026-05-13	t	2026-04-29 12:00:00+08	2026-04-29 12:00:00+08
8	16	7	2	t	隢????銝行?拚勗???脣漲??2026-05-22	f	2026-05-08 11:30:00+08	2026-05-08 11:30:00+08
9	16	7	1	t	\N	2026-05-12	t	2026-05-12 21:42:02.049491+08	2026-05-12 21:42:02.049491+08
10	16	3	1	f	\N	2026-05-12	t	2026-05-12 21:42:02.049491+08	2026-05-12 21:42:02.049491+08
11	6	7	2	t	\N	2026-05-12	f	2026-05-12 21:58:15.688577+08	2026-05-12 21:58:15.688577+08
12	6	5	2	f	\N	2026-05-12	f	2026-05-12 21:58:15.688577+08	2026-05-12 21:58:15.688577+08
13	6	7	2	t	\N	2026-05-12	t	2026-05-12 21:58:19.633772+08	2026-05-12 21:58:19.633772+08
14	6	8	2	f	\N	2026-05-12	t	2026-05-12 21:58:19.633772+08	2026-05-12 21:58:19.633772+08
15	52	7	3	t	Cloud Run smoke assign	\N	t	2026-05-28 11:34:51.607421+08	2026-05-28 11:34:51.607421+08
16	51	7	2	t	隢敹怠???2026-05-29	f	2026-05-28 11:46:49.203657+08	2026-05-28 11:46:49.203657+08
17	51	5	2	f	隢敹怠???2026-05-29	f	2026-05-28 11:46:49.203657+08	2026-05-28 11:46:49.203657+08
18	51	8	2	t	鈭活瘣曉極嚗?蝯行??2026-05-30	t	2026-05-28 13:39:40.040359+08	2026-05-28 13:39:40.040359+08
19	51	7	2	f	鈭活瘣曉極嚗?蝯行??2026-05-30	t	2026-05-28 13:39:40.040359+08	2026-05-28 13:39:40.040359+08
20	45	12	1	t	\N	2026-05-28	t	2026-05-28 13:50:31.323511+08	2026-05-28 13:50:31.323511+08
21	53	5	2	t	\N	2026-05-28	t	2026-05-28 13:55:38.138597+08	2026-05-28 13:55:38.138597+08
22	64	7	2	t	\N	2026-05-30	f	2026-05-28 16:06:32.674395+08	2026-05-28 16:06:32.674395+08
23	64	8	2	f	\N	2026-05-30	f	2026-05-28 16:06:32.674395+08	2026-05-28 16:06:32.674395+08
24	64	7	2	t	\N	2026-05-28	t	2026-05-28 16:09:49.990557+08	2026-05-28 16:09:49.990557+08
25	64	5	2	f	\N	2026-05-28	t	2026-05-28 16:09:49.990557+08	2026-05-28 16:09:49.990557+08
26	63	8	2	t	\N	2026-06-06	t	2026-05-28 17:02:06.607188+08	2026-05-28 17:02:06.607188+08
27	63	5	2	f	\N	2026-06-06	t	2026-05-28 17:02:06.607188+08	2026-05-28 17:02:06.607188+08
28	65	11	2	t	\N	2026-05-28	t	2026-05-28 17:13:13.522226+08	2026-05-28 17:13:13.522226+08
29	66	7	2	t	\N	2026-05-28	t	2026-05-28 17:18:55.372692+08	2026-05-28 17:18:55.372692+08
30	66	5	2	f	\N	2026-05-28	t	2026-05-28 17:18:55.372692+08	2026-05-28 17:18:55.372692+08
31	66	8	2	f	\N	2026-05-28	t	2026-05-28 17:18:55.372692+08	2026-05-28 17:18:55.372692+08
32	67	7	2	t	\N	2026-05-28	f	2026-05-28 20:00:34.67457+08	2026-05-28 20:00:34.67457+08
33	67	8	2	f	\N	2026-05-28	f	2026-05-28 20:00:34.67457+08	2026-05-28 20:00:34.67457+08
34	67	5	2	f	\N	2026-05-28	f	2026-05-28 20:00:34.67457+08	2026-05-28 20:00:34.67457+08
35	67	7	2	t	\N	2026-05-28	t	2026-05-28 20:00:38.551197+08	2026-05-28 20:00:38.551197+08
36	67	5	2	f	\N	2026-05-28	t	2026-05-28 20:00:38.551197+08	2026-05-28 20:00:38.551197+08
37	69	7	2	t	\N	2026-05-28	t	2026-05-28 20:28:00.447741+08	2026-05-28 20:28:00.447741+08
38	69	8	2	f	\N	2026-05-28	t	2026-05-28 20:28:00.447741+08	2026-05-28 20:28:00.447741+08
39	69	5	2	f	\N	2026-05-28	t	2026-05-28 20:28:00.447741+08	2026-05-28 20:28:00.447741+08
\.


--
-- Data for Name: case_estimations; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.case_estimations (estimation_id, case_id, estimator_user_id, seq_no, request_date, summary, estimated_hours, reply_date, estimation_status, remarks, created_at, updated_at, case_log_id) FROM stdin;
1	16	1	1	2026-05-12	test	9999.00	2026-05-12	30	\N	2026-05-12 20:26:32.33402+08	2026-05-12 20:26:32.33402+08	\N
2	16	1	2	2026-05-12	123	11.00	2026-05-12	30	\N	2026-05-12 22:44:41.651231+08	2026-05-12 22:44:41.651231+08	\N
3	16	1	3	2026-05-12	123	110.00	2026-05-12	30	\N	2026-05-12 22:49:17.812563+08	2026-05-12 22:49:17.812563+08	\N
4	16	1	4	2026-05-12	1234	8.00	2026-05-12	30	\N	2026-05-12 23:16:42.854756+08	2026-05-12 23:16:42.854756+08	20
\.


--
-- Data for Name: case_logs; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.case_logs (log_id, case_id, handler_user_id, log_date, handling_method, handling_result, hours_spent, headcount, status_after, created_at, updated_at, eval_hours, ref_case_id) FROM stdin;
1	8	11	2026-03-28	餈質馱 NullPointerException嚗????唳?雿?contract_owner_id ?刻?鞈???NULL ?芸? null-check??\N	2.00	1	30	2026-03-28 20:45:00+08	2026-03-28 20:45:00+08	\N	\N
2	8	11	2026-03-30	敶靽桀儔?批捆銝血???霅??摰極??撌脫 ContractController.list ?? Optional ??嚗蒂鋆??桀?皜祈岫?otfix 撌脫?嗆 18:00 銝???3.00	1	40	2026-03-30 22:45:00+08	2026-03-30 22:45:00+08	\N	\N
3	9	11	2026-04-02	靘?瘜銵??歹?摰Ｘ?箸鞈??蝝???桃????瑁?? 4 撘菔”??\N	2.00	1	30	2026-04-02 17:00:00+08	2026-04-02 17:00:00+08	\N	\N
4	9	11	2026-04-04	敶靽桀儔?批捆銝血???霅??摰極??撌脣????????歹???皜皜???閮?蝔賣????3.00	1	40	2026-04-04 19:00:00+08	2026-04-04 19:00:00+08	\N	\N
5	10	7	2026-04-08	瑼Ｚ? application server ??DB connection pool 閮剖?嚗??max-pool-size 銝雲隞交?葉???陸?潦?\N	2.00	1	30	2026-04-08 18:30:00+08	2026-04-08 18:30:00+08	\N	\N
6	10	7	2026-04-10	敶靽桀儔?批捆銝血???霅??摰極??撌脣? connection pool 敺?30 隤輯 80嚗蒂??????亙熒瑼Ｘ嚗??閫撖?3 憭拇?儔?整?3.00	1	40	2026-04-10 20:30:00+08	2026-04-10 20:30:00+08	\N	\N
7	11	11	2026-04-18	?勗? 02:00~04:30 ?瑁? reindex 雿平嚗??揣撘?撱箏??ize 蝮桀? 18%??\N	2.00	1	30	2026-04-18 21:00:00+08	2026-04-18 21:00:00+08	\N	\N
8	11	11	2026-04-20	敶靽桀儔?批捆銝血???霅??摰極??撌脩??撱箏??隢?PM 蝣箄?敺?獢?3.00	1	40	2026-04-20 23:00:00+08	2026-04-20 23:00:00+08	\N	\N
9	12	11	2026-04-21	?潛 CRM ??鞎餌頂蝯曹??亦?閮摨??典恥?園?憭扳???摨??????撖怨??歇????撠?\N	2.00	1	30	2026-04-21 15:20:00+08	2026-04-21 15:20:00+08	\N	\N
10	12	11	2026-04-23	敶靽桀儔?批捆銝血???霅??摰極??摰?隞?Ⅳ靽格嚗蒂?? 3 憭拇風?脰???甇?? PM 蝣箄?敺?獢?3.00	1	40	2026-04-23 17:20:00+08	2026-04-23 17:20:00+08	\N	\N
11	14	7	2026-04-25	敺?APM 閫撖 query plan 霈?嚗?甇交???恥?園?憓?撠蝝Ｗ??豢??刻粥?航楝敺歇??explain plan 瘥???\N	2.00	1	30	2026-04-25 14:15:00+08	2026-04-25 14:15:00+08	\N	\N
12	15	7	2026-05-01	?臬摰Ｘ?亥孛蝝?銝剔匱銵剁?靘??璆剖???ID ????\N	2.00	1	30	2026-05-01 14:00:00+08	2026-05-01 14:00:00+08	\N	\N
13	17	1	2026-05-12	99		9999.99	1	30	2026-05-12 20:25:31.423551+08	2026-05-12 20:25:31.423551+08	\N	\N
14	16	1	2026-05-12	test		9999.00	1	30	2026-05-12 20:26:32.305711+08	2026-05-12 20:26:32.305711+08	\N	\N
15	29	5	2026-05-12	123		0.00	1	30	2026-05-12 22:43:41.280158+08	2026-05-12 22:43:41.280158+08	\N	\N
16	29	5	2026-05-12	13		11.00	1	30	2026-05-12 22:43:52.266122+08	2026-05-12 22:43:52.266122+08	\N	\N
17	16	1	2026-05-12	11		0.00	1	30	2026-05-12 22:44:23.30552+08	2026-05-12 22:44:23.30552+08	\N	\N
18	16	1	2026-05-12	123		111.00	1	30	2026-05-12 22:44:41.628378+08	2026-05-12 22:44:41.628378+08	\N	\N
19	16	1	2026-05-12	123		110.00	1	30	2026-05-12 22:49:17.49878+08	2026-05-12 22:49:17.49878+08	\N	\N
20	16	1	2026-05-12	1234		80.00	1	30	2026-05-12 23:16:42.53118+08	2026-05-12 23:16:42.53118+08	\N	\N
21	16	1	2026-05-12	11111		0.00	1	30	2026-05-12 23:16:54.211554+08	2026-05-12 23:16:54.211554+08	\N	\N
22	52	3	2026-05-28	Cloud Run smoke log	OK	0.50	1	30	2026-05-28 11:34:51.742295+08	2026-05-28 11:34:51.742295+08	\N	\N
23	51	7	2026-05-28	??頛詨3.15		3.15	1	30	2026-05-28 13:46:03.982049+08	2026-05-28 13:46:03.982049+08	\N	\N
24	51	7	2026-05-28	撌交?9999		9999.00	1	30	2026-05-28 13:46:27.087256+08	2026-05-28 13:46:27.087256+08	\N	\N
25	51	8	2026-05-28	閮剖?摰?		2.00	1	30	2026-05-28 13:47:24.951867+08	2026-05-28 13:47:24.951867+08	\N	\N
26	45	12	2026-05-28	靽格迤bug		10.00	1	30	2026-05-28 13:51:12.872403+08	2026-05-28 13:51:12.872403+08	\N	\N
27	53	5	2026-05-28	test		10.00	1	30	2026-05-28 14:46:44.422941+08	2026-05-28 14:46:44.422941+08	\N	\N
28	6	5	2026-05-28	test		10.00	1	30	2026-05-28 15:17:14.125293+08	2026-05-28 15:17:14.125293+08	\N	\N
29	64	2	2026-05-28	蝚砌?甈∪?閬?	1.00	1	30	2026-05-28 17:00:39.748603+08	2026-05-28 17:00:39.748603+08	\N	\N
30	64	5	2026-05-28	蝚砌?甈∪?閬?	0.00	1	30	2026-05-28 17:00:55.715863+08	2026-05-28 17:00:55.715863+08	\N	\N
31	64	5	2026-05-28	鋆極??	1.50	1	30	2026-05-28 17:01:16.367773+08	2026-05-28 17:01:16.367773+08	\N	\N
32	63	5	2026-05-28	隤踵1??	2.50	1	30	2026-05-28 17:02:48.15753+08	2026-05-28 17:02:48.15753+08	\N	\N
33	66	5	2026-05-28	DDD		1.00	1	30	2026-05-28 17:19:55.942465+08	2026-05-28 17:19:55.942465+08	\N	\N
34	65	2	2026-05-28	敶靽桀儔?批捆銝血???霅??摰極??	0.00	1	30	2026-05-28 17:21:15.330513+08	2026-05-28 17:21:15.330513+08	\N	10
35	67	2	2026-05-28	蝚砌?甈∪?閬?	0.00	1	30	2026-05-28 20:10:35.407401+08	2026-05-28 20:10:35.407401+08	\N	\N
\.


--
-- Data for Name: case_replies; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.case_replies (reply_id, case_id, replier_user_id, reply_date, reply_content, created_at, updated_at) FROM stdin;
1	9	11	2026-04-01	撌脣????芷雿平嚗??斗??桀歇????2026-04-01 13:00:00+08	2026-04-01 13:00:00+08
2	9	3	2026-04-01	撌脩Ⅱ隤?雓???2026-04-01 13:00:00+08	2026-04-01 13:00:00+08
3	10	3	2026-04-07	撌脫?堆?撌亦?撣急??芸?????2026-04-07 14:30:00+08	2026-04-07 14:30:00+08
4	11	11	2026-04-17	蝝Ｗ??遣撌脣???size 蝮桀? 18%嚗?航炊??2026-04-17 17:00:00+08	2026-04-17 17:00:00+08
5	12	11	2026-04-20	撌脣?????隢?PM 蝣箄?敺?獢?2026-04-20 11:20:00+08	2026-04-20 11:20:00+08
6	13	2	2026-04-22	摰Ｘ靘銵函內撌脰銵?蝵桀?蝣潘??砍????2026-04-22 15:30:00+08	2026-04-22 15:30:00+08
7	14	3	2026-04-24	撌脫?堆?撌亦?撣急迤?冽??乩葉嚗?蝥????湔?脣漲??2026-04-24 10:15:00+08	2026-04-24 10:15:00+08
8	15	7	2026-04-30	鞈??渡?銝哨?撌脣????銝????敺予?臬?典???2026-04-30 10:00:00+08	2026-04-30 10:00:00+08
9	16	2	2026-05-09	撌脫?瘣?nami ?脰?閰摯嚗?閮?5/22 ??閬?隡啣??2026-05-09 09:30:00+08	2026-05-09 09:30:00+08
\.


--
-- Data for Name: case_type_projects; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.case_type_projects (id, type_id, project_id) FROM stdin;
1	1	1
2	1	2
3	1	3
4	1	4
5	1	5
6	2	1
7	2	2
8	2	3
9	2	4
10	2	5
11	3	1
12	3	2
13	3	3
14	3	4
15	3	5
16	4	1
17	4	2
18	4	3
19	4	4
20	4	5
21	5	1
22	5	2
23	5	3
24	5	4
25	5	5
26	1	6
27	2	6
28	3	6
29	4	6
30	5	6
\.


--
-- Data for Name: case_types; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.case_types (type_id, code, label, description, color, sort_order, is_active, created_at, updated_at) FROM stdin;
1	REPAIR	??隤踵	\N	bg-red-100 text-red-800	10	t	2026-05-27 12:55:58.488553	2026-05-28 17:15:44.350255
2	EVALUATION	撌交?閰摯	\N	bg-purple-100 text-purple-800	20	t	2026-05-27 12:55:58.488553	2026-05-28 17:15:44.350255
3	MAINTENANCE	?亙虜蝬剝?	\N	bg-blue-100 text-blue-800	30	t	2026-05-27 12:55:58.488553	2026-05-28 17:15:44.350255
4	UHD	UHD?	\N	bg-teal-100 text-teal-800	40	t	2026-05-27 12:55:58.488553	2026-05-28 17:15:44.350255
5	INQUIRY	銝?祈岷??\N	bg-sky-100 text-sky-800	50	t	2026-05-27 12:55:58.488553	2026-05-28 17:15:44.350255
\.


--
-- Data for Name: cases; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.cases (case_id, case_number, project_id, customer_id, category_id, module_id, reporter_name, reporter_phone, reporter_email, case_type, priority, description, status, created_by, assigned_pm_id, closed_by, cancelled_by, related_case_id, relation_type, closed_at, cancelled_at, created_at, updated_at, due_at, total_hours) FROM stdin;
1	SM-MAINT-202604-001	1	1	3	1	??憍?\N	lin.yt@oo-mart.com.tw	REPAIR	MEDIUM	摰Ｘ?頧?啣?摨??店?釭?＊銝?嚗?瑞?蝥?10	2	\N	\N	\N	\N	\N	\N	\N	2026-04-07 17:26:00+08	2026-04-07 17:26:00+08	\N	0.00
2	SM-MAINT-202604-002	1	1	1	3	?喳???0933-111-003	\N	REPAIR	MEDIUM	5/10 ?啁??砌?蝺?嚗??圈店閮??亥岷?暺??閰Ｕ???鞈?銝?征?踝?雿?DB ?亙??啗??獄?拍敹急炎閬?蝡航? API 銋???????質?????甈???賢?????10	2	\N	\N	\N	\N	\N	\N	\N	2026-04-09 16:23:00+08	2026-04-09 16:23:00+08	\N	0.00
3	SM-MAINT-202604-003	1	1	12	\N	撘萇???\N	\N	MAINTENANCE	MEDIUM	隢??拚?蝵桀恥?犖??cs0017 撖Ⅳ??10	2	\N	\N	\N	\N	\N	\N	\N	2026-04-10 14:34:00+08	2026-04-10 14:34:00+08	\N	0.00
4	SM-MAINT-202604-004	1	1	6	2	暺???0933-111-004	huang.sf@oo-mart.com.tw	EVALUATION	MEDIUM	撣?閰摯?啣??恥??璈??瘣整??踝?靘??蝺?瘜???????舫?雿?????閰摯?撌交??銵扼?10	2	\N	\N	\N	\N	\N	\N	\N	2026-04-14 09:46:00+08	2026-04-14 09:46:00+08	\N	0.00
5	SM-MAINT-202604-005	1	1	14	3	?遣摰?0933-111-006	\N	MAINTENANCE	MEDIUM	隢?靽??輻?皜 2024 撟游漲隞亙??店?瑼?靽??? 18 ??撌脤?嚗?? NAS 蝛粹????文?隢??臬蝝Ｗ?皜蝯行???摮?10	2	\N	\N	\N	\N	\N	\N	\N	2026-04-18 08:14:00+08	2026-04-18 08:14:00+08	\N	0.00
7	SM-MAINT-202605-002	1	1	25	2	?喃???\N	wu.jj@oo-mart.com.tw	UHD	MEDIUM	摰Ｘ?唾迄?株? CS-2026-0345 敺?靽格迤??鈭箏?箝?摨恥??蝮賢?詨恥??隢??拙???DB 靽格迤??10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-08 17:11:00+08	2026-05-08 17:11:00+08	\N	0.00
8	TC-CRM-202603-001	2	2	1	\N	蝪∪?撘?0934-222-003	chien.cc@xx-telecom.com.tw	REPAIR	MEDIUM	5/8 銝?敺?璆剖?鈭箏????蝞∠??撠梯歲 HTTP 500??蝡?Log ????NullPointerException ??ContractController.list??50	2	2	2	\N	\N	\N	2026-04-01 16:45:00+08	\N	2026-03-25 16:45:00+08	2026-03-25 16:45:00+08	\N	5.00
9	TC-CRM-202603-002	2	2	20	\N	????\N	hsueh.yf@xx-telecom.com.tw	UHD	MEDIUM	靘?瘜?嚗???芷摰Ｘ ID A123456789 銋????閮???50	3	3	3	\N	\N	\N	2026-04-06 13:00:00+08	\N	2026-03-30 13:00:00+08	2026-03-30 13:00:00+08	\N	5.00
10	TC-CRM-202604-001	2	2	4	\N	?剖?鞊?0934-222-001	cheng.zh@xx-telecom.com.tw	REPAIR	MEDIUM	璆剖?鈭箏?餃 CRM 敺??脣?摰Ｘ?箸鞈??歲?箝瘜?鞈?摨怒隤歹??岫 2~3 甈∪??舀??蔣?輻???隞銝剖????貊??潛???50	2	2	2	\N	\N	\N	2026-04-12 14:30:00+08	\N	2026-04-05 14:30:00+08	2026-04-05 14:30:00+08	\N	5.00
11	TC-CRM-202604-002	2	2	17	\N	??瞏?\N	fu.xj@xx-telecom.com.tw	MAINTENANCE	MEDIUM	5/1 ??靘???DB 蝝Ｗ??遣雿平撌脫銝勗??瑁?摰?嚗?蝣箄?蝯?銝衣?獢?40	3	3	\N	\N	\N	\N	\N	\N	2026-04-15 17:00:00+08	2026-04-15 17:00:00+08	\N	5.00
12	TC-CRM-202604-003	2	2	3	\N	擐桃???\N	feng.yz@xx-telecom.com.tw	REPAIR	MEDIUM	CRM ??鞎餌頂蝯曹??亦?摰Ｘ撣喳?鞈?撠?朣?CRM 憿舐內撌脩像雿?鞎餌頂蝯曹?憿舐內?芰像???亙歇?嗅 5 隞嗅恥?嗥閮氬?40	3	3	\N	\N	\N	\N	\N	\N	2026-04-18 11:20:00+08	2026-04-18 11:20:00+08	\N	5.00
13	TC-CRM-202604-004	2	2	12	\N	??蝧?\N	\N	MAINTENANCE	MEDIUM	隢?蝵格平?犖??sales042 銋?蝣潦?60	2	2	\N	2	\N	\N	\N	2026-04-20 19:30:00+08	2026-04-20 15:30:00+08	2026-04-20 15:30:00+08	\N	0.00
14	TC-CRM-202604-005	2	2	2	\N	瞏∪?	\N	\N	REPAIR	MEDIUM	璆剖??亥岷隞???漲?＊霈嚗?? 1~2 蝘辣?瑕 8~10 蝘?30	3	3	\N	\N	\N	\N	\N	\N	2026-04-22 10:15:00+08	2026-04-22 10:15:00+08	\N	2.00
15	TC-CRM-202604-006	2	2	21	\N	?梁???0934-222-008	\N	UHD	MEDIUM	璆剖??刻???Q1 蝮暹?瑼Ｘ嚗???? 1/1~3/31 ????平?犖?∩?摰Ｘ?亥孛蝝??excel嚗?30	3	3	\N	\N	\N	\N	\N	\N	2026-04-28 10:00:00+08	2026-04-28 10:00:00+08	\N	2.00
18	BK-ATM-202604-001	3	3	1	8	蝝靽?	0935-333-003	chi.jh@triangle-bank.com.tw	REPAIR	MEDIUM	5/9 ?銝?敺?5/9 ?亦??亦?撠董瑼鋆賢仃??鞎∪??函瘜??亦??歇?典?銝?亥????嚗?隞銝????閬??唳炎閬雿? jobflow 銝剜??10	3	\N	\N	\N	\N	\N	\N	\N	2026-04-05 15:46:00+08	2026-04-05 15:46:00+08	\N	0.00
19	BK-ATM-202604-002	3	3	7	9	璆???0935-333-006	yang.yw@triangle-bank.com.tw	EVALUATION	MEDIUM	?恣?閬?瘙雯?憭折?鈭斗??撘瑕???摮?霅?撣?閰摯?函??OTP 憭?????FIDO2 蝖祇???賊????澆極???嗆?敶梢???澆?勗??閰摯?勗???10	3	\N	\N	\N	\N	\N	\N	\N	2026-04-07 09:36:00+08	2026-04-07 09:36:00+08	\N	0.00
20	BK-ATM-202604-003	3	3	2	6	?賡???\N	pai.yc@triangle-bank.com.tw	REPAIR	MEDIUM	ATM 擗??亥岷憿舐內???航炊嚗?鈭?$1,000?恥?嗆?閮氬?10	3	\N	\N	\N	\N	\N	\N	\N	2026-04-09 15:13:00+08	2026-04-09 15:13:00+08	\N	0.00
21	BK-ATM-202604-004	3	3	3	6	?湔???0935-333-001	tai.wj@triangle-bank.com.tw	REPAIR	MEDIUM	頝刻?頧董????批仃??摰Ｘ??蝝? 10 蝑停??1 蝑＊蝷箝漱?暹?嚗?瘣賢恥??雿祕?甈曉歇???◢?迎?摰Ｘ?航??頧董???芸?????10	3	\N	\N	\N	\N	\N	\N	\N	2026-04-22 19:28:00+08	2026-04-22 19:28:00+08	\N	0.00
22	BK-ATM-202604-005	3	3	2	5	樴???\N	\N	REPAIR	MEDIUM	摰Ｘ蝬脤??餃??啁?蝪∟? OTP 撱園 3~5 ????嚗蔣?輻?仿?撽?10	3	\N	\N	\N	\N	\N	\N	\N	2026-04-28 19:40:00+08	2026-04-28 19:40:00+08	\N	0.00
23	BK-ATM-202605-001	3	3	12	\N	?⊥?蝧?\N	\N	MAINTENANCE	MEDIUM	隢??拚?蝵株??∪極??BK1023 銋?蝣潦?10	3	\N	\N	\N	\N	\N	\N	\N	2026-05-07 14:21:00+08	2026-05-07 14:21:00+08	\N	0.00
24	LG-DISP-202603-001	4	4	3	\N	敺???\N	\N	REPAIR	MEDIUM	?豢? App ?冽蝬脤蝺????亙?蝪賣敺????????鞈??郊憭望?嚗???????銝??10	6	\N	\N	\N	\N	\N	\N	\N	2026-03-30 19:27:00+08	2026-03-30 19:27:00+08	\N	0.00
25	LG-DISP-202604-001	4	4	22	\N	擃???\N	kao.cw@square-logistics.com.tw	UHD	MEDIUM	???LG-2026-08821 摰Ｘ蝪賣?抒?銝?航炊嚗??啣隞塚?嚗??敺?啣??箸迤蝣箇??抒????血?????10	6	\N	\N	\N	\N	\N	\N	\N	2026-04-08 11:51:00+08	2026-04-08 11:51:00+08	\N	0.00
26	LG-DISP-202604-002	4	4	1	\N	????\N	su.wl@square-logistics.com.tw	REPAIR	MEDIUM	5/7 銝?敺??楝敺???????撌桃憭改??璈??閬?頝舐?蝜?頝胯???routing engine ???祈???臬?郊??10	6	\N	\N	\N	\N	\N	\N	\N	2026-04-10 19:22:00+08	2026-04-10 19:22:00+08	\N	0.00
27	LG-DISP-202604-003	4	4	2	\N	敶剜???0936-444-001	peng.wc@square-logistics.com.tw	REPAIR	MEDIUM	隞?拍瘣曉極敺??潛??8 撘菟??瘣暸???A ??瘣曉 B ??豢???10	5	\N	\N	\N	\N	\N	\N	\N	2026-04-14 18:08:00+08	2026-04-14 18:08:00+08	\N	0.00
28	LG-DISP-202604-004	4	4	2	\N	閮梢???0936-444-003	hsu.cy@square-logistics.com.tw	REPAIR	MEDIUM	?梯”?臬 Excel 敺?雿鈭??隞嗡犖??雿??啜????雿?敶梢鞎∪?????10	5	\N	\N	\N	\N	\N	\N	\N	2026-04-25 11:08:00+08	2026-04-25 11:08:00+08	\N	0.00
16	TC-CRM-202605-001	2	2	6	\N	??遣??0934-222-005	\N	EVALUATION	MEDIUM	撣?閰摯?啣??恥?嗅?蝢扎??踝?靘?鞎餉??箄??閮 VIP / 銝??/ 瘚仃?郎銝黎????撌交?閰摯??30	2	1	\N	\N	\N	\N	\N	\N	2026-05-07 09:30:00+08	2026-05-12 23:16:54.21408+08	\N	10300.00
30	FB-POS-202603-001	5	5	3	\N	蝡仿???\N	\N	REPAIR	MEDIUM	??摨?株????孵??游蝮賢?詨?嚗???蝡舀?梯”??撠?銝?撌桃蝝?0.5% ~ 1%??10	4	\N	\N	\N	\N	\N	\N	\N	2026-03-31 12:55:00+08	2026-03-31 12:55:00+08	\N	0.00
31	FB-POS-202604-001	5	5	2	\N	?振??0937-555-001	fan.jw@diamond-food.com.tw	REPAIR	MEDIUM	?踵?摨?POS 蝯董??潛巨憭望?嚗??唳?憿舐內???暹???雿祕??撣唾??歇撖怠??憭拙歇蝬洵銝活??10	4	\N	\N	\N	\N	\N	\N	\N	2026-04-04 12:40:00+08	2026-04-04 12:40:00+08	\N	0.00
32	FB-POS-202604-002	5	5	6	\N	撠文?敹?\N	yu.hc@diamond-food.com.tw	EVALUATION	MEDIUM	撣?閰摯?啣????⊿?暺??踝??臬 POS 蝯董?? QR 蝣潛敞蝛??暺?????閰摯??10	4	\N	\N	\N	\N	\N	\N	\N	2026-04-19 12:21:00+08	2026-04-19 12:21:00+08	\N	0.00
33	FB-POS-202604-003	5	5	1	\N	銝???0937-555-005	\N	REPAIR	MEDIUM	5/6 銝?敺????蝞撣賂??孵?蝯? ( 蝚砌?隞?6 ??+ 皛踹?? ) 蝞靘?憿?撠 50 ??10	4	\N	\N	\N	\N	\N	\N	\N	2026-04-27 11:03:00+08	2026-04-27 11:03:00+08	\N	0.00
34	FB-POS-202604-004	5	5	24	\N	?游???\N	\N	UHD	MEDIUM	5/15 ?踹??砍?隤踵擗ㄡ璆剔?璆剔??賊?閮剖?嚗???湔??摨?POS ?????賂?銝行?質??亙??典?????10	4	\N	\N	\N	\N	\N	\N	\N	2026-04-30 18:41:00+08	2026-04-30 18:41:00+08	\N	0.00
35	FB-POS-202604-005	5	5	3	\N	鞈湔∪?	0937-555-003	lai.yc@diamond-food.com.tw	REPAIR	MEDIUM	蝺?暺?撟喳閮?嗥瘝??郊??POS嚗?撱瞍嚗恥?嗅摨蝑??圈????亙歇??4 隞嗅恥閮氬??芸?????10	4	\N	\N	\N	\N	\N	\N	\N	2026-04-30 19:25:00+08	2026-04-30 19:25:00+08	\N	0.00
17	TC-CRM-202605-002	2	2	4	\N	??蝧?0934-222-009	yeh.tj@xx-telecom.com.tw	REPAIR	MEDIUM	璆剖?鈭箏?隞?餃 CRM 敺?澆?暸??銝剜嚗??岫 1~2 甈⊥??賣?????擃???銝准?30	2	\N	\N	\N	\N	\N	\N	\N	2026-05-11 08:30:00+08	2026-05-12 20:25:31.580771+08	\N	9999.99
29	LG-DISP-202604-005	4	4	13	\N	蝧???0936-444-005	\N	MAINTENANCE	MEDIUM	隢??拇 PP 瘣曉極??冽?臬??策?豢? D087嚗璈????嗅???30	5	\N	\N	\N	\N	\N	\N	\N	2026-04-26 19:37:00+08	2026-05-12 22:43:52.267941+08	\N	11.00
36	SM-MAINT-202605-003	1	1	7	4	Gino			EVALUATION	MEDIUM	皜祈岫閰摯	60	2	\N	\N	2	\N	\N	\N	2026-05-25 10:14:47.773196+08	2026-05-25 17:52:47.744811+08	2026-05-25 18:14:47.791388+08	\N	0.00
37	SM-MAINT-202605-004	1	1	6	4	Gino			EVALUATION	MEDIUM	皜祈岫閰摯	10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-25 10:15:36.901076+08	2026-05-25 10:15:36.901076+08	\N	0.00
38	SM-MAINT-202605-005	1	1	7	4	Gino			EVALUATION	MEDIUM	皜祈岫閰摯	10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-26 03:08:30.269531+08	2026-05-26 03:08:30.269531+08	\N	0.00
39	SM-MAINT-202605-006	1	1	1	\N	brian			REPAIR	MEDIUM	TEST	10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-27 05:56:04.898669+08	2026-05-27 05:56:04.898669+08	\N	0.00
40	SM-MAINT-202605-007	1	1	1	\N	brian			REPAIR	MEDIUM	TEST	10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-27 05:57:39.175777+08	2026-05-27 05:57:39.175777+08	\N	0.00
41	SM-MAINT-202605-008	1	1	12	4	GINO			MAINTENANCE	MEDIUM	皜祈岫	10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-27 05:58:19.113291+08	2026-05-27 05:58:19.113291+08	\N	0.00
42	SM-MAINT-202605-009	1	1	12	4	GINO			MAINTENANCE	MEDIUM	TEST	10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-27 05:59:08.695516+08	2026-05-27 05:59:08.695516+08	\N	0.00
43	OE-202605-001	6	1	12	\N	皜祈岫	\N	\N	MAINTENANCE	MEDIUM	OE 蝺函Ⅳ皜祈岫	10	1	\N	\N	\N	\N	\N	\N	\N	2026-05-27 06:16:02.360276+08	2026-05-27 06:16:02.360276+08	\N	0.00
44	SM-MAINT-202605-010	1	1	6	4	gino			EVALUATION	MEDIUM	test	10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-27 06:28:12.286087+08	2026-05-27 06:28:12.286087+08	\N	0.00
46	OE-202605-002	6	1	12	\N	??閮箸	\N	\N	MAINTENANCE	MEDIUM	TZ test	10	1	\N	\N	\N	\N	\N	\N	\N	2026-05-27 07:17:59.328835+08	2026-05-27 07:17:59.328835+08	\N	0.00
47	OE-202605-003	6	1	12	\N	TZ靽桀儔撽?	\N	\N	MAINTENANCE	MEDIUM	after Timezone=UTC fix	10	1	\N	\N	\N	\N	\N	\N	\N	2026-05-27 15:20:01.887998+08	2026-05-27 15:20:01.887998+08	\N	0.00
48	LG-DISP-202605-001	4	4	12	\N	gino			MAINTENANCE	MEDIUM	?蔭??	10	1	\N	\N	\N	\N	\N	\N	\N	2026-05-27 15:23:51.73284+08	2026-05-27 15:23:51.73284+08	\N	0.00
49	SM-202605-001	1	1	12	\N	?凋誨??霅?\N	\N	MAINTENANCE	MEDIUM	expect SM-202605-001	10	1	\N	\N	\N	\N	\N	\N	\N	2026-05-27 16:24:07.094805+08	2026-05-27 16:24:07.094805+08	\N	0.00
50	BK-202605-001	3	3	12	\N	gino			MAINTENANCE	MEDIUM	撖Ⅳ?蔭	10	1	\N	\N	\N	\N	\N	\N	\N	2026-05-27 18:04:22.547691+08	2026-05-27 18:04:22.547691+08	\N	0.00
52	BK-202605-002	3	3	1	6	CloudRun Probe	0900000000	probe@example.com	REPAIR	MEDIUM	Cloud Run smoke test 2026-05-28T03:34:50.416Z	60	3	3	\N	3	\N	\N	\N	2026-05-28 11:34:51.901146+08	2026-05-28 11:34:50.575028+08	2026-05-28 11:34:51.907919+08	\N	0.00
55	SM-202605-005	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 14:47:34.949919+08	2026-05-28 14:47:34.949919+08	\N	0.00
56	SM-202605-006	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 14:49:18.240027+08	2026-05-28 14:49:18.240027+08	\N	0.00
51	SM-202605-002	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_20260528	30	2	2	\N	\N	\N	\N	\N	\N	2026-05-28 10:12:14.716769+08	2026-05-28 13:47:24.953357+08	\N	0.00
45	BK-ATM-202605-002	3	3	20	6	GINO			UHD	MEDIUM	瘨祥??	30	1	1	\N	\N	\N	\N	\N	\N	2026-05-27 06:59:22.723017+08	2026-05-28 13:51:12.874229+08	\N	0.00
53	SM-202605-003	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	50	2	2	5	\N	\N	\N	2026-05-28 14:47:12.383102+08	\N	2026-05-28 13:55:33.521144+08	2026-05-28 14:47:12.385917+08	\N	0.00
54	SM-202605-004	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 14:47:30.821079+08	2026-05-28 14:47:30.821079+08	\N	0.00
57	SM-202605-007	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 14:56:58.772424+08	2026-05-28 14:56:58.772424+08	\N	0.00
58	SM-202605-008	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 14:57:19.536504+08	2026-05-28 14:57:19.536504+08	\N	0.00
59	SM-202605-009	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 14:59:52.408423+08	2026-05-28 14:59:52.408423+08	\N	0.00
6	SM-MAINT-202605-001	1	1	2	3	????0933-111-001	wang.xm@oo-mart.com.tw	REPAIR	MEDIUM	摰Ｘ?銝剖??店蝯?敺??瑼??喳敺 NAS 憭望?嚗??乩?????30% ?店瘝??Ｙ??瑼?敶梢摰Ｚ迄蝔賣????交???銝血?鋆??交?剝??單???30	2	2	\N	\N	\N	\N	\N	\N	2026-05-07 11:55:00+08	2026-05-28 15:17:14.424379+08	\N	0.00
60	SM-202605-010	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 15:17:35.278806+08	2026-05-28 15:17:35.278806+08	\N	0.00
61	SM-202605-011	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 15:17:55.291056+08	2026-05-28 15:17:55.291056+08	\N	0.00
62	SM-202605-012	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	10	5	\N	\N	\N	53	REOPEN	\N	\N	2026-05-28 15:18:19.357423+08	2026-05-28 15:18:19.357423+08	\N	0.00
64	SM-202605-014	1	1	14	\N	Brian			MAINTENANCE	MEDIUM	??皜祈岫?亙虜蝬剝?	50	2	2	2	\N	\N	\N	2026-05-28 17:01:35.372191+08	\N	2026-05-28 16:03:58.547559+08	2026-05-28 17:01:35.374558+08	\N	0.00
63	SM-202605-013	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	50	2	2	2	\N	53	REOPEN	2026-05-28 17:09:25.83175+08	\N	2026-05-28 15:20:55.77485+08	2026-05-28 17:09:26.00249+08	\N	0.00
66	SM-202605-015	1	1	1	\N	Brian			REPAIR	MEDIUM	??撱箇?獢辣_2026052822222	40	2	2	\N	\N	\N	\N	\N	\N	2026-05-28 17:18:50.085056+08	2026-05-28 17:20:00.524763+08	\N	0.00
65	TC-202605-001	2	2	6	\N	??遣??0934-222-005		EVALUATION	MEDIUM	撣?閰摯?啣??恥?嗅?蝢扎??踝?靘?鞎餉??箄??閮 VIP / 銝??/ 瘚仃?郎銝黎????撌交?閰摯??30	2	2	\N	\N	\N	\N	\N	\N	2026-05-28 17:13:00.112375+08	2026-05-28 17:21:15.334735+08	\N	0.00
67	SM-202605-016	1	1	14	\N	Brian			MAINTENANCE	MEDIUM	??皜祈岫?亙虜蝬剝?	50	2	2	2	\N	\N	\N	2026-05-28 20:10:46.767162+08	\N	2026-05-28 20:00:21.924478+08	2026-05-28 20:10:46.769519+08	\N	0.00
68	SM-202605-017	1	1	11	\N	brain			EVALUATION	MEDIUM	DDD	10	2	\N	\N	\N	\N	\N	\N	\N	2026-05-28 20:15:33.568324+08	2026-05-28 20:15:33.568324+08	\N	0.00
69	SM-202605-018	1	1	6	4	yyy			EVALUATION	MEDIUM	test	20	2	2	\N	\N	\N	\N	\N	\N	2026-05-28 20:27:56.718532+08	2026-05-28 20:28:00.453864+08	\N	0.00
\.


--
-- Data for Name: customers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.customers (customer_id, customer_name, contact_person, contact_phone, contact_email, address, remarks, is_active, created_at, updated_at) FROM stdin;
1	OO頞??∩遢???砍	????02-2701-1111	service@oo-mart.com.tw	?啣?撣縑蝢拙?撣?憭折?鈭挾100???嗅璆剖恥??(Demo data)	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
2	XX?颱縑?∩遢???砍	????02-2702-2222	support@xx-telecom.com.tw	?啣?撣皝???頝?00???颱縑璆剖恥??(Demo data)	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
3	?喇?平?銵隞賣?????喳??02-2703-3333	ops@triangle-bank.com.tw	?啣?撣葉甇????楝銝畾?0???銵平摰Ｘ (Demo data)	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
4	?﹦?拇??∩遢???砍	撘菔???03-3704-4444	service@square-logistics.com.tw	獢?撣葉憯Ｗ?銝剖控頝?00???拇?璆剖恥??(Demo data)	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
5	?????擗ㄡ?∩遢???砍	????02-2705-5555	it@diamond-food.com.tw	?啣?撣璈???頝臭?畾?00??擗ㄡ璆剖恥??(Demo data)	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
\.


--
-- Data for Name: notifications; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.notifications (notification_id, recipient_user_id, case_id, notification_type, title, message, is_read, read_at, created_at) FROM stdin;
1	7	16	CASE_ASSIGNED	獢辣瘣曉極 TC-CRM-202605-001	?冽??啁?獢辣敺???f	\N	2026-05-12 21:42:02.049491+08
2	3	16	CASE_ASSIGNED	獢辣瘣曉極 TC-CRM-202605-001	?冽??啁?獢辣敺???f	\N	2026-05-12 21:42:02.049491+08
3	7	6	CASE_ASSIGNED	獢辣瘣曉極 SM-MAINT-202605-001	?冽??啁?獢辣敺???f	\N	2026-05-12 21:58:15.688577+08
5	7	6	CASE_ASSIGNED	獢辣瘣曉極 SM-MAINT-202605-001	?冽??啁?獢辣敺???f	\N	2026-05-12 21:58:19.633772+08
6	8	6	CASE_ASSIGNED	獢辣瘣曉極 SM-MAINT-202605-001	?冽??啁?獢辣敺???f	\N	2026-05-12 21:58:19.633772+08
7	2	\N	CASE_CREATED	?唳?隞?SM-MAINT-202605-003	?唳?隞嗅歇撱箇?嚗葫閰西?隡?t	2026-05-26 01:53:08.144842+08	2026-05-25 17:52:47.744811+08
8	2	36	CASE_CANCELLED	獢辣?? SM-MAINT-202605-003	獢辣撌脰◤??	f	\N	2026-05-25 10:14:47.773196+08
9	2	\N	CASE_CREATED	?唳?隞?SM-MAINT-202605-004	?唳?隞嗅歇撱箇?嚗葫閰西?隡?f	\N	2026-05-25 10:15:36.901076+08
10	2	\N	CASE_CREATED	?唳?隞?SM-MAINT-202605-005	?唳?隞嗅歇撱箇?嚗葫閰西?隡?f	\N	2026-05-26 03:08:30.269531+08
11	2	\N	CASE_CREATED	?唳?隞?SM-MAINT-202605-006	?唳?隞嗅歇撱箇?嚗EST	f	\N	2026-05-27 05:56:04.898669+08
12	2	\N	CASE_CREATED	?唳?隞?SM-MAINT-202605-007	?唳?隞嗅歇撱箇?嚗EST	f	\N	2026-05-27 05:57:39.175777+08
13	2	\N	CASE_CREATED	?唳?隞?SM-MAINT-202605-008	?唳?隞嗅歇撱箇?嚗葫閰?f	\N	2026-05-27 05:58:19.113291+08
14	2	\N	CASE_CREATED	?唳?隞?SM-MAINT-202605-009	?唳?隞嗅歇撱箇?嚗EST	f	\N	2026-05-27 05:59:08.695516+08
15	2	\N	CASE_CREATED	?唳?隞?SM-MAINT-202605-010	?唳?隞嗅歇撱箇?嚗est	f	\N	2026-05-27 06:28:12.286087+08
16	3	\N	CASE_CREATED	?唳?隞?BK-ATM-202605-002	?唳?隞嗅歇撱箇?嚗?鞎餃?憿?f	\N	2026-05-27 06:59:22.723017+08
18	6	\N	CASE_CREATED	?唳?隞?LG-DISP-202605-001	?唳?隞嗅歇撱箇?嚗?蝵桀?憿?f	\N	2026-05-27 15:23:51.73284+08
19	2	\N	CASE_CREATED	?唳?隞?SM-202605-001	?唳?隞嗅歇撱箇?嚗xpect SM-202605-001	f	\N	2026-05-27 16:24:07.094805+08
20	3	\N	CASE_CREATED	?唳?隞?BK-202605-001	?唳?隞嗅歇撱箇?嚗?蝣潮?蝵?f	\N	2026-05-27 18:04:22.547691+08
21	2	\N	CASE_CREATED	?唳?隞?SM-202605-002	?唳?隞嗅歇撱箇?嚗??遣蝡?隞跛20260528	f	\N	2026-05-28 10:12:14.716769+08
22	3	\N	CASE_CREATED	?唳?隞?BK-202605-002	?唳?隞嗅歇撱箇?嚗loud Run smoke test 2026-05-28T03:34:50.416Z	f	\N	2026-05-28 11:34:50.575028+08
24	7	52	CASE_CANCELLED	獢辣?? BK-202605-002	Cloud Run smoke cleanup	f	\N	2026-05-28 11:34:51.901146+08
25	3	52	CASE_CANCELLED	獢辣?? BK-202605-002	Cloud Run smoke cleanup	f	\N	2026-05-28 11:34:51.901146+08
30	12	45	CASE_ASSIGNED	獢辣瘣曉極 BK-ATM-202605-002	?冽??啁?獢辣敺???f	\N	2026-05-28 13:50:31.323511+08
28	8	51	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-002	?冽??啁?獢辣敺???t	2026-05-28 13:52:45.807402+08	2026-05-28 13:39:40.040359+08
29	7	51	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-002	?冽??啁?獢辣敺???t	2026-05-28 13:53:41.478199+08	2026-05-28 13:39:40.040359+08
26	7	51	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-002	?冽??啁?獢辣敺???t	2026-05-28 13:53:44.816169+08	2026-05-28 11:46:49.203657+08
23	7	52	CASE_ASSIGNED	獢辣瘣曉極 BK-202605-002	?冽??啁?獢辣敺???t	2026-05-28 13:53:55.052697+08	2026-05-28 11:34:51.607421+08
27	5	51	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-002	?冽??啁?獢辣敺???t	2026-05-28 13:54:25.512846+08	2026-05-28 11:46:49.203657+08
31	2	\N	CASE_CREATED	?唳?隞?SM-202605-003	?唳?隞嗅歇撱箇?嚗??遣蝡?隞跛2026052822222	f	\N	2026-05-28 13:55:33.521144+08
32	5	53	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-003	?冽??啁?獢辣敺???t	2026-05-28 13:55:44.202851+08	2026-05-28 13:55:38.138597+08
17	5	\N	CASE_CREATED	?唳?隞?LG-DISP-202605-001	?唳?隞嗅歇撱箇?嚗?蝵桀?憿?t	2026-05-28 14:45:24.06003+08	2026-05-27 15:23:51.73284+08
4	5	6	CASE_ASSIGNED	獢辣瘣曉極 SM-MAINT-202605-001	?冽??啁?獢辣敺???t	2026-05-28 14:45:25.619842+08	2026-05-12 21:58:15.688577+08
33	2	53	WORK_COMPLETED	獢辣摰極 SM-202605-003	獢辣撌脣?撌伐?隢Ⅱ隤?衣?獢?f	\N	2026-05-28 14:46:50.825526+08
34	2	\N	CASE_CREATED	?唳?隞?SM-202605-014	?唳?隞嗅歇撱箇?嚗??葫閰行撣貊雁??f	\N	2026-05-28 16:03:58.547559+08
35	7	64	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-014	?冽??啁?獢辣敺???f	\N	2026-05-28 16:06:32.674395+08
36	8	64	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-014	?冽??啁?獢辣敺???f	\N	2026-05-28 16:06:32.674395+08
37	7	64	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-014	?冽??啁?獢辣敺???f	\N	2026-05-28 16:09:49.990557+08
38	5	64	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-014	?冽??啁?獢辣敺???f	\N	2026-05-28 16:09:49.990557+08
39	2	64	WORK_COMPLETED	獢辣摰極 SM-202605-014	獢辣撌脣?撌伐?隢Ⅱ隤?衣?獢?f	\N	2026-05-28 17:01:23.447487+08
40	8	63	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-013	?冽??啁?獢辣敺???f	\N	2026-05-28 17:02:06.607188+08
41	5	63	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-013	?冽??啁?獢辣敺???f	\N	2026-05-28 17:02:06.607188+08
42	2	63	WORK_COMPLETED	獢辣摰極 SM-202605-013	獢辣撌脣?撌伐?隢Ⅱ隤?衣?獢?f	\N	2026-05-28 17:02:57.930178+08
43	2	\N	CASE_CREATED	?唳?隞?TC-202605-001	?唳?隞嗅歇撱箇?嚗???隡唳憓恥?嗅?蝢扎??踝?靘?鞎餉??箄??閮 VIP / 銝??/ 瘚仃?郎銝黎????撌交?閰摯??f	\N	2026-05-28 17:13:00.112375+08
44	3	\N	CASE_CREATED	?唳?隞?TC-202605-001	?唳?隞嗅歇撱箇?嚗???隡唳憓恥?嗅?蝢扎??踝?靘?鞎餉??箄??閮 VIP / 銝??/ 瘚仃?郎銝黎????撌交?閰摯??f	\N	2026-05-28 17:13:00.112375+08
45	11	65	CASE_ASSIGNED	獢辣瘣曉極 TC-202605-001	?冽??啁?獢辣敺???f	\N	2026-05-28 17:13:13.522226+08
46	2	\N	CASE_CREATED	?唳?隞?SM-202605-015	?唳?隞嗅歇撱箇?嚗??遣蝡?隞跛2026052822222	f	\N	2026-05-28 17:18:50.085056+08
47	7	66	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-015	?冽??啁?獢辣敺???f	\N	2026-05-28 17:18:55.372692+08
48	5	66	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-015	?冽??啁?獢辣敺???f	\N	2026-05-28 17:18:55.372692+08
49	8	66	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-015	?冽??啁?獢辣敺???f	\N	2026-05-28 17:18:55.372692+08
50	2	66	WORK_COMPLETED	獢辣摰極 SM-202605-015	獢辣撌脣?撌伐?隢Ⅱ隤?衣?獢?f	\N	2026-05-28 17:20:00.523289+08
51	2	\N	CASE_CREATED	?唳?隞?SM-202605-016	?唳?隞嗅歇撱箇?嚗??葫閰行撣貊雁??f	\N	2026-05-28 20:00:21.924478+08
52	7	67	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-016	?冽??啁?獢辣敺???f	\N	2026-05-28 20:00:34.67457+08
53	8	67	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-016	?冽??啁?獢辣敺???f	\N	2026-05-28 20:00:34.67457+08
54	5	67	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-016	?冽??啁?獢辣敺???f	\N	2026-05-28 20:00:34.67457+08
55	7	67	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-016	?冽??啁?獢辣敺???f	\N	2026-05-28 20:00:38.551197+08
56	5	67	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-016	?冽??啁?獢辣敺???f	\N	2026-05-28 20:00:38.551197+08
57	2	67	WORK_COMPLETED	獢辣摰極 SM-202605-016	獢辣撌脣?撌伐?隢Ⅱ隤?衣?獢?f	\N	2026-05-28 20:10:42.993085+08
58	2	\N	CASE_CREATED	?唳?隞?SM-202605-017	?唳?隞嗅歇撱箇?嚗DD	f	\N	2026-05-28 20:15:33.568324+08
59	2	\N	CASE_CREATED	?唳?隞?SM-202605-018	?唳?隞嗅歇撱箇?嚗est	f	\N	2026-05-28 20:27:56.718532+08
60	7	69	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-018	?冽??啁?獢辣敺???f	\N	2026-05-28 20:28:00.447741+08
61	8	69	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-018	?冽??啁?獢辣敺???f	\N	2026-05-28 20:28:00.447741+08
62	5	69	CASE_ASSIGNED	獢辣瘣曉極 SM-202605-018	?冽??啁?獢辣敺???f	\N	2026-05-28 20:28:00.447741+08
\.


--
-- Data for Name: problem_categories; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.problem_categories (category_id, category_name, description, sort_order, is_active, created_at, updated_at, parent_type, case_type_filter, project_id) FROM stdin;
1	?銝??啣虜	\N	1	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	REPAIR	\N
2	?亙虜???啣虜	\N	2	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	REPAIR	\N
3	銝誨隞?啣虜	\N	3	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	REPAIR	\N
4	DB????啣虜	\N	4	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	REPAIR	\N
5	?嗡?	\N	99	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	REPAIR	\N
6	?圈?瘙??潸?隡?\N	1	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	EVALUATION	\N
7	蝟餌絞?孵?閰摯	\N	2	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	EVALUATION	\N
8	鞈??啣?閰摯	\N	3	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	EVALUATION	\N
9	??皜祈岫閰摯	\N	4	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	EVALUATION	\N
10	隞隤踵閰摯	\N	5	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	EVALUATION	\N
11	?嗡?	\N	99	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	EVALUATION	\N
12	鈭箏撖Ⅳ?蔭	\N	1	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	MAINTENANCE	\N
13	PP瘣曉極?隤踵	\N	2	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	MAINTENANCE	\N
14	甇瑕鞈?皜	\N	3	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	MAINTENANCE	\N
15	??蝟餌絞皜祈岫	\N	4	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	MAINTENANCE	\N
16	鞈?隤踵	\N	5	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	MAINTENANCE	\N
17	摰?鞈?瑼Ｘ	\N	6	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	MAINTENANCE	\N
18	?嗡?	\N	99	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	MAINTENANCE	\N
19	?拙硫?鞎券甈曇???\N	1	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	UHD	\N
20	瘨祥?????\N	2	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	UHD	\N
21	??鞈?	\N	3	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	UHD	\N
22	鞈??啣?	\N	4	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	UHD	\N
23	?桐?鞈???	\N	5	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	UHD	\N
24	蝟餌絞?閮剖?	\N	6	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	UHD	\N
25	敺DB鞈?靽格	\N	7	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	UHD	\N
26	?嗡?	\N	99	t	2026-05-11 10:09:07.671502+08	2026-05-11 10:09:07.671502+08	\N	UHD	\N
\.


--
-- Data for Name: problem_category_case_types; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.problem_category_case_types (id, category_id, type_id) FROM stdin;
1	5	1
2	4	1
3	3	1
4	2	1
5	1	1
6	11	2
7	10	2
8	9	2
9	8	2
10	7	2
11	6	2
12	18	3
13	17	3
14	16	3
15	15	3
16	14	3
17	13	3
18	12	3
19	26	4
20	25	4
21	24	4
22	23	4
23	22	4
24	21	4
25	20	4
26	19	4
\.


--
-- Data for Name: problem_category_projects; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.problem_category_projects (id, category_id, project_id) FROM stdin;
1	1	1
2	1	2
3	1	3
4	1	4
5	1	5
6	2	1
7	2	2
8	2	3
9	2	4
10	2	5
11	3	1
12	3	2
13	3	3
14	3	4
15	3	5
16	4	1
17	4	2
18	4	3
19	4	4
20	4	5
21	5	1
22	5	2
23	5	3
24	5	4
25	5	5
26	6	1
27	6	2
28	6	3
29	6	4
30	6	5
31	7	1
32	7	2
33	7	3
34	7	4
35	7	5
36	8	1
37	8	2
38	8	3
39	8	4
40	8	5
41	9	1
42	9	2
43	9	3
44	9	4
45	9	5
46	10	1
47	10	2
48	10	3
49	10	4
50	10	5
51	11	1
52	11	2
53	11	3
54	11	4
55	11	5
56	12	1
57	12	2
58	12	3
59	12	4
60	12	5
61	13	1
62	13	2
63	13	3
64	13	4
65	13	5
66	14	1
67	14	2
68	14	3
69	14	4
70	14	5
71	15	1
72	15	2
73	15	3
74	15	4
75	15	5
76	16	1
77	16	2
78	16	3
79	16	4
80	16	5
81	17	1
82	17	2
83	17	3
84	17	4
85	17	5
86	18	1
87	18	2
88	18	3
89	18	4
90	18	5
91	19	1
92	19	2
93	19	3
94	19	4
95	19	5
96	20	1
97	20	2
98	20	3
99	20	4
100	20	5
101	21	1
102	21	2
103	21	3
104	21	4
105	21	5
106	22	1
107	22	2
108	22	3
109	22	4
110	22	5
111	23	1
112	23	2
113	23	3
114	23	4
115	23	5
116	24	1
117	24	2
118	24	3
119	24	4
120	24	5
121	25	1
122	25	2
123	25	3
124	25	4
125	25	5
126	26	1
127	26	2
128	26	3
129	26	4
130	26	5
131	1	6
132	2	6
133	3	6
134	4	6
135	5	6
136	6	6
137	7	6
138	8	6
139	9	6
140	10	6
141	11	6
142	12	6
143	13	6
144	14	6
145	15	6
146	16	6
147	17	6
148	18	6
149	19	6
150	20	6
151	21	6
152	22	6
153	23	6
154	24	6
155	25	6
156	26	6
\.


--
-- Data for Name: project_codes; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.project_codes (code_id, code, label, description, sort_order, is_active, created_at, updated_at) FROM stdin;
6	OE	OO?餃???撟喳	皜祈岫銝?撠?	10	t	2026-05-27 14:15:03.103442	2026-05-27 14:15:03.103442
1	BK	?喇?銵雯頝涉TM蝬剛風獢?\N	0	t	2026-05-27 14:14:38.390371	2026-05-27 16:23:21.793229
2	FB	?????擗ㄡPOS撠獢?\N	0	t	2026-05-27 14:14:38.390371	2026-05-27 16:23:21.793229
3	LG	?﹦?拇??矽摨衣頂蝯梁雁霅瑟?	\N	0	t	2026-05-27 14:14:38.390371	2026-05-27 16:23:21.793229
4	SM	OO頞?閰勗?蝟餌絞蝬剛風獢?\N	0	t	2026-05-27 14:14:38.390371	2026-05-27 16:23:21.793229
5	TC	XX?颱縑摰Ｘ??蝟餌絞蝬剝?獢?\N	0	t	2026-05-27 14:14:38.390371	2026-05-27 16:23:21.793229
7	BK-ATM	BK-ATM	\N	0	t	2026-05-28 11:33:52.442121	2026-05-28 11:33:52.442121
8	FB-POS	FB-POS	\N	0	t	2026-05-28 11:33:52.442121	2026-05-28 11:33:52.442121
9	LG-DISP	LG-DISP	\N	0	t	2026-05-28 11:33:52.442121	2026-05-28 11:33:52.442121
10	SM-MAINT	SM-MAINT	\N	0	t	2026-05-28 11:33:52.442121	2026-05-28 11:33:52.442121
11	TC-CRM	TC-CRM	\N	0	t	2026-05-28 11:33:52.442121	2026-05-28 11:33:52.442121
\.


--
-- Data for Name: project_members; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.project_members (member_id, project_id, user_id, member_role, joined_at, is_active, created_at) FROM stdin;
1	1	2	PM	2025-12-01	t	2026-05-12 10:00:00+08
2	1	7	SE	2025-12-01	t	2026-05-12 10:00:00+08
3	1	8	SE	2025-12-05	t	2026-05-12 10:00:00+08
4	1	5	SE	2026-01-10	t	2026-05-12 10:00:00+08
5	2	2	PM	2025-10-01	t	2026-05-12 10:00:00+08
6	2	3	PM	2025-10-15	t	2026-05-12 10:00:00+08
7	2	7	SE	2025-10-01	t	2026-05-12 10:00:00+08
8	2	11	SE	2025-10-20	t	2026-05-12 10:00:00+08
9	3	3	PM	2026-01-01	t	2026-05-12 10:00:00+08
10	3	6	SE	2026-01-05	t	2026-05-12 10:00:00+08
11	3	8	SE	2026-01-10	t	2026-05-12 10:00:00+08
12	3	12	SE	2026-02-01	t	2026-05-12 10:00:00+08
13	4	5	PM	2025-09-01	t	2026-05-12 10:00:00+08
14	4	6	PM	2025-09-15	t	2026-05-12 10:00:00+08
15	4	9	SE	2025-09-01	t	2026-05-12 10:00:00+08
16	4	13	SE	2025-10-01	t	2026-05-12 10:00:00+08
17	4	14	SE	2025-11-01	t	2026-05-12 10:00:00+08
18	5	4	PM	2026-03-01	t	2026-05-12 10:00:00+08
19	5	10	SE	2026-03-01	t	2026-05-12 10:00:00+08
20	5	12	SE	2026-03-15	t	2026-05-12 10:00:00+08
21	5	13	SE	2026-04-01	t	2026-05-12 10:00:00+08
\.


--
-- Data for Name: projects; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.projects (project_id, project_code, project_name, customer_id, description, start_date, end_date, is_active, created_at, updated_at, allowed_case_types, project_code_id) FROM stdin;
1	SM-MAINT	OO頞?閰勗?蝟餌絞蝬剛風獢?1	OO頞?摰Ｘ?銝剖?閰勗?蝟餌絞撟游漲蝬剝?嚗?店??恥?恣?銵冽閰ｇ?	2025-12-01	2026-11-30	t	2026-05-12 10:00:00+08	2026-05-27 14:14:38.390371+08	\N	4
2	TC-CRM	XX?颱縑摰Ｘ??蝟餌絞蝬剝?獢?2	XX?颱縑摰Ｘ?? CRM 蝟餌絞蝬剝???撟??2025-10-01	2026-09-30	t	2026-05-12 10:00:00+08	2026-05-27 14:14:38.390371+08	\N	5
3	BK-ATM	?喇?銵雯頝涉TM蝬剛風獢?3	?喇?銵雯頝?ATM + 頝刻?皜?隞蝬剝?獢?2026-01-01	2026-12-31	t	2026-05-12 10:00:00+08	2026-05-27 14:14:38.390371+08	\N	1
4	LG-DISP	?﹦?拇??矽摨衣頂蝯梁雁霅瑟?	4	?﹦?拇??矽摨衣頂蝯勗僑摨衣雁霅?2025-09-01	2026-08-31	t	2026-05-12 10:00:00+08	2026-05-27 14:14:38.390371+08	\N	3
5	FB-POS	?????擗ㄡPOS撠獢?5	?????擗ㄡ?典?? POS 蝟餌絞撠?帘摰?	2026-03-01	2026-12-31	t	2026-05-12 10:00:00+08	2026-05-27 14:14:38.390371+08	\N	2
6	OE	OO?餃???撟喳	1	皜祈岫銝?撠?	2026-05-31	\N	t	2026-05-27 14:15:03.35755+08	2026-05-27 14:27:23.268677+08	\N	6
\.


--
-- Data for Name: schema_migrations; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.schema_migrations (version, description, installed_on) FROM stdin;
3	add_total_hours_to_cases	2026-05-11 10:09:07.6697
4	problem_categories_by_case_type	2026-05-11 10:09:07.743713
5	fix_case_type_check_constraint	2026-05-11 10:09:07.747529
6	add_eval_hours_to_case_logs	2026-05-11 10:09:07.748582
\.


--
-- Data for Name: system_modules; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.system_modules (module_id, project_id, module_name, description, is_active, created_at, updated_at) FROM stdin;
1	1	閰勗??店璅∠?	摰Ｘ??店?交????t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
2	1	摰Ｘ?蝞∠?璅∠?	摰Ｘ?鈭箏蝞∠??極??t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
3	1	?店?璅∠?	?店?摮??閰?t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
4	1	?梯”?亥岷璅∠?	摰Ｘ?閰梢??蜀?銵?t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
5	3	蝬脰楝?銵芋蝯?摰Ｘ蝡舐雯?鈭斗?	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
6	3	ATM鈭斗?璅∠?	ATM 蝡臭漱??皜?	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
7	3	頝刻?皜?璅∠?	?瓷?楊銵?蝞???t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
8	3	撠董瑼芋蝯??亦?撠董瑼鋆質?銝?	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
9	3	??摮?霅芋蝯?OTP / 蝪∟?撽?	t	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08
\.


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.users (user_id, username, password_hash, full_name, email, phone, role, is_active, last_login_at, created_at, updated_at, must_change_password, google_sub, google_email, auth_provider) FROM stdin;
4	naruto	@sld123456	瞍拇蒂曈港犖	naruto@caseflow.local	0911-100-003	PM	t	\N	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08	f	\N	\N	local
6	tanjiro	@sld123456	蝡??剜祥??tanjiro@caseflow.local	0911-100-005	PM	t	\N	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08	f	\N	\N	local
9	sakura	@sld123456	?仿?瑹?sakura@caseflow.local	0922-200-003	SE	t	\N	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08	f	\N	\N	local
10	goku	@sld123456	摮急?蝛?goku@caseflow.local	0922-200-004	SE	t	\N	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08	f	\N	\N	local
13	nezuko	@sld123456	蝡?蝳啗?摮?nezuko@caseflow.local	0922-200-007	SE	t	\N	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08	f	\N	\N	local
14	ichigo	@sld123456	暺?銝霅?ichigo@caseflow.local	0922-200-008	SE	t	\N	2026-05-12 10:00:00+08	2026-05-12 10:00:00+08	f	\N	\N	local
12	ran	AQAAAAIAAYagAAAAENaDFsKG+/aNf0nlyFRDuabf6zR/MPSye8qGY25CM0zeURsXwUYUC05vVzJqOZSK/g==	瘥??ran@caseflow.local	0922-200-006	SE	t	2026-05-28 13:50:42.310914+08	2026-05-12 10:00:00+08	2026-05-28 13:50:42.411521+08	f	\N	\N	local
3	zoro	AQAAAAIAAYagAAAAEL6bI2fJRryC2/12LVRrzwT3EJAfnHGve9UPHC6rk2hYiTINPqku0ICD+Hge+2USjg==	蝢?鈭瑞揣??zoro@caseflow.local	0911-100-002	PM	t	2026-05-28 17:15:56.744905+08	2026-05-12 10:00:00+08	2026-05-28 17:15:56.766117+08	f	\N	\N	local
1	admin	AQAAAAIAAYagAAAAEJcTzidWL/X8IanODKn+/6lCl+1mWW/BzmB2VS3qWnRqQMmzGUP7Nht/dbyZuhr1RA==	蝟餌絞蝞∠???admin@caseflow.local	02-2700-0000	SysAdmin	t	2026-05-28 16:54:52.347019+08	2026-05-12 10:00:00+08	2026-05-28 16:54:52.348035+08	f	\N	\N	local
8	sasuke	AQAAAAIAAYagAAAAENPIiGboeJtunuayCWcxFWJZYekqitqjg1VIKtRvXoxJvWD1p3UF5mRSeaPyzswWPw==	摰瘜Ｖ???sasuke@caseflow.local	0922-200-002	SE	t	2026-05-28 20:08:09.722925+08	2026-05-12 10:00:00+08	2026-05-28 20:08:09.725265+08	f	\N	\N	local
11	vegeta	AQAAAAIAAYagAAAAEAHUHBo4+GhINoYQ0eoN5f3pbDp+xmXZWrbNVHQplh7+10/Y8xVQ1v+enc5dTGTcAw==	?	vegeta@caseflow.local	0922-200-005	SE	t	2026-05-28 20:08:38.153136+08	2026-05-12 10:00:00+08	2026-05-28 20:08:38.154986+08	f	\N	\N	local
2	luffy	AQAAAAIAAYagAAAAECyHQh7ncAaTMl3h2UNFcq0bjuWdOg4L/eqFu3z6FPXXD7GyTZEXqvcoRl+OcYuAtg==	?繚D繚擳臬井	luffy@caseflow.local	0911-100-001	PM	t	2026-05-28 20:27:40.992988+08	2026-05-12 10:00:00+08	2026-05-28 20:27:40.993905+08	f	\N	\N	local
7	nami	AQAAAAIAAYagAAAAEGEZEK+46reA98ObZFg0RxfX7eWbim2kMpbjgRbxpYVaC9zP3e8q+oaDeMkzzlWU4A==	憡?	nami@caseflow.local	0922-200-001	SE	t	2026-05-28 20:28:34.546469+08	2026-05-12 10:00:00+08	2026-05-28 20:28:34.548553+08	f	\N	\N	local
5	conan	AQAAAAIAAYagAAAAEJ73NQ8/h62QJLzfIKjNTIuRnZ2/pp3n9hkvGxDvZNUaJew9MWnRS5R+yXgBgXPe+g==	瘙撌??conan@caseflow.local	0911-100-004	PM	t	2026-05-28 20:55:04.877704+08	2026-05-12 10:00:00+08	2026-05-28 20:55:04.987029+08	f	\N	\N	local
\.


--
-- Name: Customers_customer_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Customers_customer_id_seq"', 5, true);


--
-- Name: DataProtectionKeys_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."DataProtectionKeys_Id_seq"', 1, true);


--
-- Name: Users_userid_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."Users_userid_seq"', 14, true);


--
-- Name: attachments_attachment_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.attachments_attachment_id_seq', 7, true);


--
-- Name: audit_logs_audit_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.audit_logs_audit_id_seq', 101, true);


--
-- Name: case_assignments_assignment_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.case_assignments_assignment_id_seq', 39, true);


--
-- Name: case_estimations_estimation_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.case_estimations_estimation_id_seq', 4, true);


--
-- Name: case_logs_log_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.case_logs_log_id_seq', 35, true);


--
-- Name: case_replies_reply_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.case_replies_reply_id_seq', 9, true);


--
-- Name: case_type_projects_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.case_type_projects_id_seq', 30, true);


--
-- Name: case_types_type_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.case_types_type_id_seq', 5, true);


--
-- Name: cases_case_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.cases_case_id_seq', 69, true);


--
-- Name: member_member_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.member_member_id_seq', 21, true);


--
-- Name: notifications_notification_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.notifications_notification_id_seq', 62, true);


--
-- Name: problem_categories_category_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.problem_categories_category_id_seq', 26, true);


--
-- Name: problem_category_case_types_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.problem_category_case_types_id_seq', 26, true);


--
-- Name: problem_category_projects_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.problem_category_projects_id_seq', 156, true);


--
-- Name: project_codes_code_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.project_codes_code_id_seq', 11, true);


--
-- Name: project_project_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.project_project_id_seq', 6, true);


--
-- Name: system_modules_module_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.system_modules_module_id_seq', 9, true);


--
-- Name: customers Customers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customers
    ADD CONSTRAINT "Customers_pkey" PRIMARY KEY (customer_id);


--
-- Name: DataProtectionKeys DataProtectionKeys_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."DataProtectionKeys"
    ADD CONSTRAINT "DataProtectionKeys_pkey" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: users Users_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT "Users_pkey" PRIMARY KEY (user_id);


--
-- Name: attachments attachments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attachments
    ADD CONSTRAINT attachments_pkey PRIMARY KEY (attachment_id);


--
-- Name: audit_logs audit_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_pkey PRIMARY KEY (audit_id);


--
-- Name: case_assignments case_assignments_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_assignments
    ADD CONSTRAINT case_assignments_pkey PRIMARY KEY (assignment_id);


--
-- Name: case_estimations case_estimations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_estimations
    ADD CONSTRAINT case_estimations_pkey PRIMARY KEY (estimation_id);


--
-- Name: case_logs case_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_logs
    ADD CONSTRAINT case_logs_pkey PRIMARY KEY (log_id);


--
-- Name: case_replies case_replies_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_replies
    ADD CONSTRAINT case_replies_pkey PRIMARY KEY (reply_id);


--
-- Name: case_type_projects case_type_projects_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_type_projects
    ADD CONSTRAINT case_type_projects_pkey PRIMARY KEY (id);


--
-- Name: case_types case_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_types
    ADD CONSTRAINT case_types_pkey PRIMARY KEY (type_id);


--
-- Name: cases cases_case_number_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_case_number_key UNIQUE (case_number);


--
-- Name: cases cases_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_pkey PRIMARY KEY (case_id);


--
-- Name: project_members member_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.project_members
    ADD CONSTRAINT member_pkey PRIMARY KEY (member_id);


--
-- Name: notifications notifications_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_pkey PRIMARY KEY (notification_id);


--
-- Name: problem_categories problem_categories_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_categories
    ADD CONSTRAINT problem_categories_pkey PRIMARY KEY (category_id);


--
-- Name: problem_category_case_types problem_category_case_types_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_case_types
    ADD CONSTRAINT problem_category_case_types_pkey PRIMARY KEY (id);


--
-- Name: problem_category_projects problem_category_projects_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_projects
    ADD CONSTRAINT problem_category_projects_pkey PRIMARY KEY (id);


--
-- Name: project_codes project_codes_code_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.project_codes
    ADD CONSTRAINT project_codes_code_key UNIQUE (code);


--
-- Name: project_codes project_codes_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.project_codes
    ADD CONSTRAINT project_codes_pkey PRIMARY KEY (code_id);


--
-- Name: projects project_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.projects
    ADD CONSTRAINT project_pkey PRIMARY KEY (project_id);


--
-- Name: schema_migrations schema_migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.schema_migrations
    ADD CONSTRAINT schema_migrations_pkey PRIMARY KEY (version);


--
-- Name: system_modules system_modules_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.system_modules
    ADD CONSTRAINT system_modules_pkey PRIMARY KEY (module_id);


--
-- Name: case_types uk_case_types_code; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_types
    ADD CONSTRAINT uk_case_types_code UNIQUE (code);


--
-- Name: problem_category_case_types uk_cat_ctype; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_case_types
    ADD CONSTRAINT uk_cat_ctype UNIQUE (category_id, type_id);


--
-- Name: problem_category_projects uk_cat_project; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_projects
    ADD CONSTRAINT uk_cat_project UNIQUE (category_id, project_id);


--
-- Name: problem_categories uk_category_name_parent_type; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_categories
    ADD CONSTRAINT uk_category_name_parent_type UNIQUE (category_name, parent_type);


--
-- Name: case_type_projects uk_ctype_project; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_type_projects
    ADD CONSTRAINT uk_ctype_project UNIQUE (type_id, project_id);


--
-- Name: customers uk_customers_customer_name; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.customers
    ADD CONSTRAINT uk_customers_customer_name UNIQUE (customer_name);


--
-- Name: projects uk_projects_project_code; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.projects
    ADD CONSTRAINT uk_projects_project_code UNIQUE (project_code);


--
-- Name: users uk_users_email; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT uk_users_email UNIQUE (email);


--
-- Name: users uk_users_username; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT uk_users_username UNIQUE (username);


--
-- Name: idx_assign_by; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_assign_by ON public.case_assignments USING btree (assigned_by) WITH (deduplicate_items='true');


--
-- Name: idx_assign_case; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_assign_case ON public.case_assignments USING btree (case_id, is_active) WITH (deduplicate_items='true');


--
-- Name: idx_assign_se; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_assign_se ON public.case_assignments USING btree (se_user_id, is_active) WITH (deduplicate_items='true');


--
-- Name: idx_attach_entity; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_attach_entity ON public.attachments USING btree (entity_type, entity_id) WITH (deduplicate_items='true');


--
-- Name: idx_attach_uploader; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_attach_uploader ON public.attachments USING btree (uploaded_by) WITH (deduplicate_items='true');


--
-- Name: idx_audit_action; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_action ON public.audit_logs USING btree (action, created_at) WITH (deduplicate_items='true');


--
-- Name: idx_audit_case; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_case ON public.audit_logs USING btree (case_id) WITH (deduplicate_items='true');


--
-- Name: idx_audit_entity; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_entity ON public.audit_logs USING btree (entity_type, entity_id) WITH (deduplicate_items='true');


--
-- Name: idx_audit_user; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_audit_user ON public.audit_logs USING btree (user_id, created_at) WITH (deduplicate_items='true');


--
-- Name: idx_case_estimations_case_log_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_case_estimations_case_log_id ON public.case_estimations USING btree (case_log_id);


--
-- Name: idx_cases_assigned_pm; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_assigned_pm ON public.cases USING btree (assigned_pm_id) WITH (deduplicate_items='true');


--
-- Name: idx_cases_category; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_category ON public.cases USING btree (category_id) WITH (deduplicate_items='true');


--
-- Name: idx_cases_created_at; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_created_at ON public.cases USING btree (created_at) WITH (deduplicate_items='true');


--
-- Name: idx_cases_created_by; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_created_by ON public.cases USING btree (created_by) WITH (deduplicate_items='true');


--
-- Name: idx_cases_customer; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_customer ON public.cases USING btree (customer_id) WITH (deduplicate_items='true');


--
-- Name: idx_cases_priority; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_priority ON public.cases USING btree (priority) WITH (deduplicate_items='true');


--
-- Name: idx_cases_project_created; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_project_created ON public.cases USING btree (project_id, created_at) WITH (deduplicate_items='true');


--
-- Name: idx_cases_project_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_project_status ON public.cases USING btree (project_id, status) WITH (deduplicate_items='true');


--
-- Name: idx_cases_type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cases_type ON public.cases USING btree (case_type) WITH (deduplicate_items='true');


--
-- Name: idx_cat_ctype_category; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cat_ctype_category ON public.problem_category_case_types USING btree (category_id);


--
-- Name: idx_cat_ctype_type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cat_ctype_type ON public.problem_category_case_types USING btree (type_id);


--
-- Name: idx_cat_proj_category; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cat_proj_category ON public.problem_category_projects USING btree (category_id);


--
-- Name: idx_cat_proj_project; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cat_proj_project ON public.problem_category_projects USING btree (project_id);


--
-- Name: idx_cat_sort; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_cat_sort ON public.problem_categories USING btree (sort_order, is_active) WITH (deduplicate_items='true');


--
-- Name: idx_ctype_proj_project; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ctype_proj_project ON public.case_type_projects USING btree (project_id);


--
-- Name: idx_ctype_proj_type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_ctype_proj_type ON public.case_type_projects USING btree (type_id);


--
-- Name: idx_customers_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_customers_active ON public.customers USING btree (is_active) WITH (deduplicate_items='true');


--
-- Name: idx_est_case; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_est_case ON public.case_estimations USING btree (case_id) WITH (deduplicate_items='true');


--
-- Name: idx_est_estimator; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_est_estimator ON public.case_estimations USING btree (estimator_user_id) WITH (deduplicate_items='true');


--
-- Name: idx_est_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_est_status ON public.case_estimations USING btree (estimation_status) WITH (deduplicate_items='true');


--
-- Name: idx_logs_case; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_logs_case ON public.case_logs USING btree (case_id) WITH (deduplicate_items='true');


--
-- Name: idx_logs_date; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_logs_date ON public.case_logs USING btree (log_date) WITH (deduplicate_items='true');


--
-- Name: idx_logs_handler; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_logs_handler ON public.case_logs USING btree (handler_user_id, log_date) WITH (deduplicate_items='true');


--
-- Name: idx_notif_case; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_notif_case ON public.notifications USING btree (case_id) WITH (deduplicate_items='true');


--
-- Name: idx_notif_recipient; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_notif_recipient ON public.notifications USING btree (recipient_user_id, is_read, created_at) WITH (deduplicate_items='true');


--
-- Name: idx_pm_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_pm_active ON public.project_members USING btree (project_id, member_role, is_active) WITH (deduplicate_items='true');


--
-- Name: idx_projects_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_projects_active ON public.projects USING btree (is_active) WITH (deduplicate_items='true');


--
-- Name: idx_projects_customer; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_projects_customer ON public.projects USING btree (customer_id) WITH (deduplicate_items='true');


--
-- Name: idx_replies_case; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_replies_case ON public.case_replies USING btree (case_id) WITH (deduplicate_items='true');


--
-- Name: idx_users_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_users_active ON public.users USING btree (is_active) WITH (deduplicate_items='true');


--
-- Name: idx_users_role; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_users_role ON public.users USING btree (role) WITH (deduplicate_items='true');


--
-- Name: uk_project_module; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX uk_project_module ON public.system_modules USING btree (project_id, module_name) WITH (deduplicate_items='true');


--
-- Name: uk_project_user; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX uk_project_user ON public.project_members USING btree (project_id, user_id) WITH (deduplicate_items='true');


--
-- Name: ux_users_google_sub; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX ux_users_google_sub ON public.users USING btree (google_sub) WHERE (google_sub IS NOT NULL);


--
-- Name: case_estimations trg_case_estimations_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_case_estimations_updated_at BEFORE UPDATE ON public.case_estimations FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: case_logs trg_case_logs_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_case_logs_updated_at BEFORE UPDATE ON public.case_logs FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: case_replies trg_case_replies_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_case_replies_updated_at BEFORE UPDATE ON public.case_replies FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: cases trg_cases_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_cases_updated_at BEFORE UPDATE ON public.cases FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: customers trg_customers_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_customers_updated_at BEFORE UPDATE ON public.customers FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: problem_categories trg_problem_categories_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_problem_categories_updated_at BEFORE UPDATE ON public.problem_categories FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: projects trg_projects_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_projects_updated_at BEFORE UPDATE ON public.projects FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: system_modules trg_system_modules_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_system_modules_updated_at BEFORE UPDATE ON public.system_modules FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: users trg_users_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON public.users FOR EACH ROW EXECUTE FUNCTION public.update_updated_at();


--
-- Name: attachments attachments_uploaded_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.attachments
    ADD CONSTRAINT attachments_uploaded_by_fkey FOREIGN KEY (uploaded_by) REFERENCES public.users(user_id);


--
-- Name: audit_logs audit_logs_case_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_case_id_fkey FOREIGN KEY (case_id) REFERENCES public.cases(case_id);


--
-- Name: audit_logs audit_logs_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: case_assignments case_assignments_assigned_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_assignments
    ADD CONSTRAINT case_assignments_assigned_by_fkey FOREIGN KEY (assigned_by) REFERENCES public.users(user_id);


--
-- Name: case_assignments case_assignments_case_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_assignments
    ADD CONSTRAINT case_assignments_case_id_fkey FOREIGN KEY (case_id) REFERENCES public.cases(case_id);


--
-- Name: case_assignments case_assignments_se_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_assignments
    ADD CONSTRAINT case_assignments_se_user_id_fkey FOREIGN KEY (se_user_id) REFERENCES public.users(user_id);


--
-- Name: case_estimations case_estimations_case_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_estimations
    ADD CONSTRAINT case_estimations_case_id_fkey FOREIGN KEY (case_id) REFERENCES public.cases(case_id);


--
-- Name: case_estimations case_estimations_case_log_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_estimations
    ADD CONSTRAINT case_estimations_case_log_id_fkey FOREIGN KEY (case_log_id) REFERENCES public.case_logs(log_id) ON DELETE SET NULL;


--
-- Name: case_estimations case_estimations_estimator_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_estimations
    ADD CONSTRAINT case_estimations_estimator_user_id_fkey FOREIGN KEY (estimator_user_id) REFERENCES public.users(user_id);


--
-- Name: case_logs case_logs_case_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_logs
    ADD CONSTRAINT case_logs_case_id_fkey FOREIGN KEY (case_id) REFERENCES public.cases(case_id);


--
-- Name: case_logs case_logs_handler_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_logs
    ADD CONSTRAINT case_logs_handler_user_id_fkey FOREIGN KEY (handler_user_id) REFERENCES public.users(user_id);


--
-- Name: case_logs case_logs_ref_case_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_logs
    ADD CONSTRAINT case_logs_ref_case_id_fkey FOREIGN KEY (ref_case_id) REFERENCES public.cases(case_id) ON DELETE SET NULL;


--
-- Name: case_replies case_replies_case_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_replies
    ADD CONSTRAINT case_replies_case_id_fkey FOREIGN KEY (case_id) REFERENCES public.cases(case_id);


--
-- Name: case_replies case_replies_replier_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_replies
    ADD CONSTRAINT case_replies_replier_user_id_fkey FOREIGN KEY (replier_user_id) REFERENCES public.users(user_id);


--
-- Name: case_type_projects case_type_projects_project_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_type_projects
    ADD CONSTRAINT case_type_projects_project_id_fkey FOREIGN KEY (project_id) REFERENCES public.projects(project_id) ON DELETE CASCADE;


--
-- Name: case_type_projects case_type_projects_type_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.case_type_projects
    ADD CONSTRAINT case_type_projects_type_id_fkey FOREIGN KEY (type_id) REFERENCES public.case_types(type_id) ON DELETE CASCADE;


--
-- Name: cases cases_assigned_pm_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_assigned_pm_id_fkey FOREIGN KEY (assigned_pm_id) REFERENCES public.users(user_id);


--
-- Name: cases cases_cancelled_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_cancelled_by_fkey FOREIGN KEY (cancelled_by) REFERENCES public.users(user_id);


--
-- Name: cases cases_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.problem_categories(category_id);


--
-- Name: cases cases_closed_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_closed_by_fkey FOREIGN KEY (closed_by) REFERENCES public.users(user_id);


--
-- Name: cases cases_created_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(user_id);


--
-- Name: cases cases_customer_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public.customers(customer_id);


--
-- Name: cases cases_module_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_module_id_fkey FOREIGN KEY (module_id) REFERENCES public.system_modules(module_id);


--
-- Name: cases cases_project_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_project_id_fkey FOREIGN KEY (project_id) REFERENCES public.projects(project_id);


--
-- Name: cases cases_related_case_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cases
    ADD CONSTRAINT cases_related_case_id_fkey FOREIGN KEY (related_case_id) REFERENCES public.cases(case_id);


--
-- Name: project_members fk_member_projects; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.project_members
    ADD CONSTRAINT fk_member_projects FOREIGN KEY (project_id) REFERENCES public.projects(project_id);


--
-- Name: project_members fk_member_user; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.project_members
    ADD CONSTRAINT fk_member_user FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- Name: projects fk_projects_customer; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.projects
    ADD CONSTRAINT fk_projects_customer FOREIGN KEY (customer_id) REFERENCES public.customers(customer_id);


--
-- Name: projects fk_projects_project_code; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.projects
    ADD CONSTRAINT fk_projects_project_code FOREIGN KEY (project_code_id) REFERENCES public.project_codes(code_id) ON DELETE SET NULL;


--
-- Name: system_modules fk_system_modules_project; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.system_modules
    ADD CONSTRAINT fk_system_modules_project FOREIGN KEY (project_id) REFERENCES public.projects(project_id);


--
-- Name: notifications notifications_case_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_case_id_fkey FOREIGN KEY (case_id) REFERENCES public.cases(case_id);


--
-- Name: notifications notifications_recipient_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notifications
    ADD CONSTRAINT notifications_recipient_user_id_fkey FOREIGN KEY (recipient_user_id) REFERENCES public.users(user_id);


--
-- Name: problem_categories problem_categories_project_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_categories
    ADD CONSTRAINT problem_categories_project_id_fkey FOREIGN KEY (project_id) REFERENCES public.projects(project_id) ON DELETE SET NULL;


--
-- Name: problem_category_case_types problem_category_case_types_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_case_types
    ADD CONSTRAINT problem_category_case_types_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.problem_categories(category_id) ON DELETE CASCADE;


--
-- Name: problem_category_case_types problem_category_case_types_type_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_case_types
    ADD CONSTRAINT problem_category_case_types_type_id_fkey FOREIGN KEY (type_id) REFERENCES public.case_types(type_id) ON DELETE CASCADE;


--
-- Name: problem_category_projects problem_category_projects_category_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_projects
    ADD CONSTRAINT problem_category_projects_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.problem_categories(category_id) ON DELETE CASCADE;


--
-- Name: problem_category_projects problem_category_projects_project_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.problem_category_projects
    ADD CONSTRAINT problem_category_projects_project_id_fkey FOREIGN KEY (project_id) REFERENCES public.projects(project_id) ON DELETE CASCADE;


--
-- Name: projects projects_project_code_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.projects
    ADD CONSTRAINT projects_project_code_id_fkey FOREIGN KEY (project_code_id) REFERENCES public.project_codes(code_id) ON DELETE SET NULL;


--
-- PostgreSQL database dump complete
--





