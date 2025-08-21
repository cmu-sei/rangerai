--
-- PostgreSQL database cluster dump
--

SET default_transaction_read_only = off;

SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;

--
-- Roles
--

CREATE ROLE n8n;
ALTER ROLE n8n WITH SUPERUSER INHERIT CREATEROLE CREATEDB LOGIN REPLICATION BYPASSRLS PASSWORD 'SCRAM-SHA-256$4096:V3s8PSh4atk5jL78vHhO4Q==$B5yvnnnLR3c/qCAMqqb5I9AuOTIX7AS8nzCzhHs3rs8=:wBpBRDdSI/cYCiChl8OuJ8nXF6kmIMK02RRq4Nu1jZ8=';

--
-- User Configurations
--








--
-- Databases
--

--
-- Database "template1" dump
--

\connect template1

--
-- PostgreSQL database dump
--

-- Dumped from database version 15.13 (Debian 15.13-1.pgdg120+1)
-- Dumped by pg_dump version 15.13 (Debian 15.13-1.pgdg120+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- PostgreSQL database dump complete
--

--
-- Database "n8ndb" dump
--

--
-- PostgreSQL database dump
--

-- Dumped from database version 15.13 (Debian 15.13-1.pgdg120+1)
-- Dumped by pg_dump version 15.13 (Debian 15.13-1.pgdg120+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: n8ndb; Type: DATABASE; Schema: -; Owner: n8n
--

CREATE DATABASE n8ndb WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'en_US.utf8';


ALTER DATABASE n8ndb OWNER TO n8n;

\connect n8ndb

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: annotation_tag_entity; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.annotation_tag_entity (
    id character varying(16) NOT NULL,
    name character varying(24) NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.annotation_tag_entity OWNER TO n8n;

--
-- Name: auth_identity; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.auth_identity (
    "userId" uuid,
    "providerId" character varying(64) NOT NULL,
    "providerType" character varying(32) NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.auth_identity OWNER TO n8n;

--
-- Name: auth_provider_sync_history; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.auth_provider_sync_history (
    id integer NOT NULL,
    "providerType" character varying(32) NOT NULL,
    "runMode" text NOT NULL,
    status text NOT NULL,
    "startedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "endedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    scanned integer NOT NULL,
    created integer NOT NULL,
    updated integer NOT NULL,
    disabled integer NOT NULL,
    error text
);


ALTER TABLE public.auth_provider_sync_history OWNER TO n8n;

--
-- Name: auth_provider_sync_history_id_seq; Type: SEQUENCE; Schema: public; Owner: n8n
--

CREATE SEQUENCE public.auth_provider_sync_history_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.auth_provider_sync_history_id_seq OWNER TO n8n;

--
-- Name: auth_provider_sync_history_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: n8n
--

ALTER SEQUENCE public.auth_provider_sync_history_id_seq OWNED BY public.auth_provider_sync_history.id;


--
-- Name: credentials_entity; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.credentials_entity (
    name character varying(128) NOT NULL,
    data text NOT NULL,
    type character varying(128) NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    id character varying(36) NOT NULL,
    "isManaged" boolean DEFAULT false NOT NULL
);


ALTER TABLE public.credentials_entity OWNER TO n8n;

--
-- Name: event_destinations; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.event_destinations (
    id uuid NOT NULL,
    destination jsonb NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.event_destinations OWNER TO n8n;

--
-- Name: execution_annotation_tags; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.execution_annotation_tags (
    "annotationId" integer NOT NULL,
    "tagId" character varying(24) NOT NULL
);


ALTER TABLE public.execution_annotation_tags OWNER TO n8n;

--
-- Name: execution_annotations; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.execution_annotations (
    id integer NOT NULL,
    "executionId" integer NOT NULL,
    vote character varying(6),
    note text,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.execution_annotations OWNER TO n8n;

--
-- Name: execution_annotations_id_seq; Type: SEQUENCE; Schema: public; Owner: n8n
--

CREATE SEQUENCE public.execution_annotations_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.execution_annotations_id_seq OWNER TO n8n;

--
-- Name: execution_annotations_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: n8n
--

ALTER SEQUENCE public.execution_annotations_id_seq OWNED BY public.execution_annotations.id;


--
-- Name: execution_data; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.execution_data (
    "executionId" integer NOT NULL,
    "workflowData" json NOT NULL,
    data text NOT NULL
);


ALTER TABLE public.execution_data OWNER TO n8n;

--
-- Name: execution_entity; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.execution_entity (
    id integer NOT NULL,
    finished boolean NOT NULL,
    mode character varying NOT NULL,
    "retryOf" character varying,
    "retrySuccessId" character varying,
    "startedAt" timestamp(3) with time zone,
    "stoppedAt" timestamp(3) with time zone,
    "waitTill" timestamp(3) with time zone,
    status character varying NOT NULL,
    "workflowId" character varying(36) NOT NULL,
    "deletedAt" timestamp(3) with time zone,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.execution_entity OWNER TO n8n;

--
-- Name: execution_entity_id_seq; Type: SEQUENCE; Schema: public; Owner: n8n
--

CREATE SEQUENCE public.execution_entity_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.execution_entity_id_seq OWNER TO n8n;

--
-- Name: execution_entity_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: n8n
--

ALTER SEQUENCE public.execution_entity_id_seq OWNED BY public.execution_entity.id;


--
-- Name: execution_metadata; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.execution_metadata (
    id integer NOT NULL,
    "executionId" integer NOT NULL,
    key character varying(255) NOT NULL,
    value text NOT NULL
);


ALTER TABLE public.execution_metadata OWNER TO n8n;

--
-- Name: execution_metadata_temp_id_seq; Type: SEQUENCE; Schema: public; Owner: n8n
--

CREATE SEQUENCE public.execution_metadata_temp_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.execution_metadata_temp_id_seq OWNER TO n8n;

--
-- Name: execution_metadata_temp_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: n8n
--

ALTER SEQUENCE public.execution_metadata_temp_id_seq OWNED BY public.execution_metadata.id;


--
-- Name: folder; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.folder (
    id character varying(36) NOT NULL,
    name character varying(128) NOT NULL,
    "parentFolderId" character varying(36),
    "projectId" character varying(36) NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.folder OWNER TO n8n;

--
-- Name: folder_tag; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.folder_tag (
    "folderId" character varying(36) NOT NULL,
    "tagId" character varying(36) NOT NULL
);


ALTER TABLE public.folder_tag OWNER TO n8n;

--
-- Name: insights_by_period; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.insights_by_period (
    id integer NOT NULL,
    "metaId" integer NOT NULL,
    type integer NOT NULL,
    value integer NOT NULL,
    "periodUnit" integer NOT NULL,
    "periodStart" timestamp(0) with time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.insights_by_period OWNER TO n8n;

--
-- Name: COLUMN insights_by_period.type; Type: COMMENT; Schema: public; Owner: n8n
--

COMMENT ON COLUMN public.insights_by_period.type IS '0: time_saved_minutes, 1: runtime_milliseconds, 2: success, 3: failure';


--
-- Name: COLUMN insights_by_period."periodUnit"; Type: COMMENT; Schema: public; Owner: n8n
--

COMMENT ON COLUMN public.insights_by_period."periodUnit" IS '0: hour, 1: day, 2: week';


--
-- Name: insights_by_period_id_seq; Type: SEQUENCE; Schema: public; Owner: n8n
--

ALTER TABLE public.insights_by_period ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.insights_by_period_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: insights_metadata; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.insights_metadata (
    "metaId" integer NOT NULL,
    "workflowId" character varying(16),
    "projectId" character varying(36),
    "workflowName" character varying(128) NOT NULL,
    "projectName" character varying(255) NOT NULL
);


ALTER TABLE public.insights_metadata OWNER TO n8n;

--
-- Name: insights_metadata_metaId_seq; Type: SEQUENCE; Schema: public; Owner: n8n
--

ALTER TABLE public.insights_metadata ALTER COLUMN "metaId" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public."insights_metadata_metaId_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: insights_raw; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.insights_raw (
    id integer NOT NULL,
    "metaId" integer NOT NULL,
    type integer NOT NULL,
    value integer NOT NULL,
    "timestamp" timestamp(0) with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.insights_raw OWNER TO n8n;

--
-- Name: COLUMN insights_raw.type; Type: COMMENT; Schema: public; Owner: n8n
--

COMMENT ON COLUMN public.insights_raw.type IS '0: time_saved_minutes, 1: runtime_milliseconds, 2: success, 3: failure';


--
-- Name: insights_raw_id_seq; Type: SEQUENCE; Schema: public; Owner: n8n
--

ALTER TABLE public.insights_raw ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME public.insights_raw_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: installed_nodes; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.installed_nodes (
    name character varying(200) NOT NULL,
    type character varying(200) NOT NULL,
    "latestVersion" integer DEFAULT 1 NOT NULL,
    package character varying(241) NOT NULL
);


ALTER TABLE public.installed_nodes OWNER TO n8n;

--
-- Name: installed_packages; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.installed_packages (
    "packageName" character varying(214) NOT NULL,
    "installedVersion" character varying(50) NOT NULL,
    "authorName" character varying(70),
    "authorEmail" character varying(70),
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.installed_packages OWNER TO n8n;

--
-- Name: invalid_auth_token; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.invalid_auth_token (
    token character varying(512) NOT NULL,
    "expiresAt" timestamp(3) with time zone NOT NULL
);


ALTER TABLE public.invalid_auth_token OWNER TO n8n;

--
-- Name: migrations; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.migrations (
    id integer NOT NULL,
    "timestamp" bigint NOT NULL,
    name character varying NOT NULL
);


ALTER TABLE public.migrations OWNER TO n8n;

--
-- Name: migrations_id_seq; Type: SEQUENCE; Schema: public; Owner: n8n
--

CREATE SEQUENCE public.migrations_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.migrations_id_seq OWNER TO n8n;

--
-- Name: migrations_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: n8n
--

ALTER SEQUENCE public.migrations_id_seq OWNED BY public.migrations.id;


--
-- Name: processed_data; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.processed_data (
    "workflowId" character varying(36) NOT NULL,
    context character varying(255) NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    value text NOT NULL
);


ALTER TABLE public.processed_data OWNER TO n8n;

--
-- Name: project; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.project (
    id character varying(36) NOT NULL,
    name character varying(255) NOT NULL,
    type character varying(36) NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    icon json,
    description character varying(512)
);


ALTER TABLE public.project OWNER TO n8n;

--
-- Name: project_relation; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.project_relation (
    "projectId" character varying(36) NOT NULL,
    "userId" uuid NOT NULL,
    role character varying NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.project_relation OWNER TO n8n;

--
-- Name: settings; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.settings (
    key character varying(255) NOT NULL,
    value text NOT NULL,
    "loadOnStartup" boolean DEFAULT false NOT NULL
);


ALTER TABLE public.settings OWNER TO n8n;

--
-- Name: shared_credentials; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.shared_credentials (
    "credentialsId" character varying(36) NOT NULL,
    "projectId" character varying(36) NOT NULL,
    role text NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.shared_credentials OWNER TO n8n;

--
-- Name: shared_workflow; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.shared_workflow (
    "workflowId" character varying(36) NOT NULL,
    "projectId" character varying(36) NOT NULL,
    role text NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.shared_workflow OWNER TO n8n;

--
-- Name: tag_entity; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.tag_entity (
    name character varying(24) NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    id character varying(36) NOT NULL
);


ALTER TABLE public.tag_entity OWNER TO n8n;

--
-- Name: test_case_execution; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.test_case_execution (
    id character varying(36) NOT NULL,
    "testRunId" character varying(36) NOT NULL,
    "executionId" integer,
    status character varying NOT NULL,
    "runAt" timestamp(3) with time zone,
    "completedAt" timestamp(3) with time zone,
    "errorCode" character varying,
    "errorDetails" json,
    metrics json,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    inputs json,
    outputs json
);


ALTER TABLE public.test_case_execution OWNER TO n8n;

--
-- Name: test_run; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.test_run (
    id character varying(36) NOT NULL,
    "workflowId" character varying(36) NOT NULL,
    status character varying NOT NULL,
    "errorCode" character varying,
    "errorDetails" json,
    "runAt" timestamp(3) with time zone,
    "completedAt" timestamp(3) with time zone,
    metrics json,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL
);


ALTER TABLE public.test_run OWNER TO n8n;

--
-- Name: user; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public."user" (
    id uuid DEFAULT uuid_in((OVERLAY(OVERLAY(md5((((random())::text || ':'::text) || (clock_timestamp())::text)) PLACING '4'::text FROM 13) PLACING to_hex((floor(((random() * (((11 - 8) + 1))::double precision) + (8)::double precision)))::integer) FROM 17))::cstring) NOT NULL,
    email character varying(255),
    "firstName" character varying(32),
    "lastName" character varying(32),
    password character varying(255),
    "personalizationAnswers" json,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    settings json,
    disabled boolean DEFAULT false NOT NULL,
    "mfaEnabled" boolean DEFAULT false NOT NULL,
    "mfaSecret" text,
    "mfaRecoveryCodes" text,
    role text NOT NULL,
    "lastActiveAt" date
);


ALTER TABLE public."user" OWNER TO n8n;

--
-- Name: user_api_keys; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.user_api_keys (
    id character varying(36) NOT NULL,
    "userId" uuid NOT NULL,
    label character varying(100) NOT NULL,
    "apiKey" character varying NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    scopes json
);


ALTER TABLE public.user_api_keys OWNER TO n8n;

--
-- Name: variables; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.variables (
    key character varying(50) NOT NULL,
    type character varying(50) DEFAULT 'string'::character varying NOT NULL,
    value character varying(255),
    id character varying(36) NOT NULL
);


ALTER TABLE public.variables OWNER TO n8n;

--
-- Name: webhook_entity; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.webhook_entity (
    "webhookPath" character varying NOT NULL,
    method character varying NOT NULL,
    node character varying NOT NULL,
    "webhookId" character varying,
    "pathLength" integer,
    "workflowId" character varying(36) NOT NULL
);


ALTER TABLE public.webhook_entity OWNER TO n8n;

--
-- Name: workflow_entity; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.workflow_entity (
    name character varying(128) NOT NULL,
    active boolean NOT NULL,
    nodes json NOT NULL,
    connections json NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    settings json,
    "staticData" json,
    "pinData" json,
    "versionId" character(36),
    "triggerCount" integer DEFAULT 0 NOT NULL,
    id character varying(36) NOT NULL,
    meta json,
    "parentFolderId" character varying(36) DEFAULT NULL::character varying,
    "isArchived" boolean DEFAULT false NOT NULL
);


ALTER TABLE public.workflow_entity OWNER TO n8n;

--
-- Name: workflow_history; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.workflow_history (
    "versionId" character varying(36) NOT NULL,
    "workflowId" character varying(36) NOT NULL,
    authors character varying(255) NOT NULL,
    "createdAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    "updatedAt" timestamp(3) with time zone DEFAULT CURRENT_TIMESTAMP(3) NOT NULL,
    nodes json NOT NULL,
    connections json NOT NULL
);


ALTER TABLE public.workflow_history OWNER TO n8n;

--
-- Name: workflow_statistics; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.workflow_statistics (
    count integer DEFAULT 0,
    "latestEvent" timestamp(3) with time zone,
    name character varying(128) NOT NULL,
    "workflowId" character varying(36) NOT NULL,
    "rootCount" integer DEFAULT 0
);


ALTER TABLE public.workflow_statistics OWNER TO n8n;

--
-- Name: workflows_tags; Type: TABLE; Schema: public; Owner: n8n
--

CREATE TABLE public.workflows_tags (
    "workflowId" character varying(36) NOT NULL,
    "tagId" character varying(36) NOT NULL
);


ALTER TABLE public.workflows_tags OWNER TO n8n;

--
-- Name: auth_provider_sync_history id; Type: DEFAULT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.auth_provider_sync_history ALTER COLUMN id SET DEFAULT nextval('public.auth_provider_sync_history_id_seq'::regclass);


--
-- Name: execution_annotations id; Type: DEFAULT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_annotations ALTER COLUMN id SET DEFAULT nextval('public.execution_annotations_id_seq'::regclass);


--
-- Name: execution_entity id; Type: DEFAULT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_entity ALTER COLUMN id SET DEFAULT nextval('public.execution_entity_id_seq'::regclass);


--
-- Name: execution_metadata id; Type: DEFAULT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_metadata ALTER COLUMN id SET DEFAULT nextval('public.execution_metadata_temp_id_seq'::regclass);


--
-- Name: migrations id; Type: DEFAULT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.migrations ALTER COLUMN id SET DEFAULT nextval('public.migrations_id_seq'::regclass);


--
-- Data for Name: annotation_tag_entity; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.annotation_tag_entity (id, name, "createdAt", "updatedAt") FROM stdin;
\.


--
-- Data for Name: auth_identity; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.auth_identity ("userId", "providerId", "providerType", "createdAt", "updatedAt") FROM stdin;
\.


--
-- Data for Name: auth_provider_sync_history; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.auth_provider_sync_history (id, "providerType", "runMode", status, "startedAt", "endedAt", scanned, created, updated, disabled, error) FROM stdin;
\.


--
-- Data for Name: credentials_entity; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.credentials_entity (name, data, type, "createdAt", "updatedAt", id, "isManaged") FROM stdin;
Ollama account	U2FsdGVkX19zuRaGnvpSoLwNBuFiEP7Ro0IHMUUUBVXfbxjEvhLsBtHZKjxq8P9r6HxPVEJvsMzPtrsoz3HS6A==	ollamaApi	2025-08-07 19:29:56.364+00	2025-08-07 19:29:56.361+00	IG6IHbnpp3S6McZf	f
OpenAi account	U2FsdGVkX18eTjZCaezb49Qc/lv7eRDmllQWA4lrsRPyYShI9vxZ1RQwbhQgb2TUbSQMYbpixWX35W2FaIIw6s+YEZKRtsVFAQGn7i1N1oXcKwmRuhEzov7u3PTt0NsR	openAiApi	2025-08-07 19:56:56.537+00	2025-08-07 19:57:11.155+00	cucV1OHveuszsqcf	f
QdrantApi account	U2FsdGVkX19LjBKamUqpI60pgAAkG6wMjoGCubxz1L0RUV2nD98yUhe+LqHFC5a7lhN8hBPwCiuiXSLWyD6tvB281w00XQX8fBZdhjvBif4=	qdrantApi	2025-08-07 19:58:22.659+00	2025-08-07 19:58:22.658+00	Wntvg0re72jrk9lu	f
\.


--
-- Data for Name: event_destinations; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.event_destinations (id, destination, "createdAt", "updatedAt") FROM stdin;
\.


--
-- Data for Name: execution_annotation_tags; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.execution_annotation_tags ("annotationId", "tagId") FROM stdin;
\.


--
-- Data for Name: execution_annotations; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.execution_annotations (id, "executionId", vote, note, "createdAt", "updatedAt") FROM stdin;
\.


--
-- Data for Name: execution_data; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.execution_data ("executionId", "workflowData", data) FROM stdin;
\.


--
-- Data for Name: execution_entity; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.execution_entity (id, finished, mode, "retryOf", "retrySuccessId", "startedAt", "stoppedAt", "waitTill", status, "workflowId", "deletedAt", "createdAt") FROM stdin;
\.


--
-- Data for Name: execution_metadata; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.execution_metadata (id, "executionId", key, value) FROM stdin;
\.


--
-- Data for Name: folder; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.folder (id, name, "parentFolderId", "projectId", "createdAt", "updatedAt") FROM stdin;
\.


--
-- Data for Name: folder_tag; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.folder_tag ("folderId", "tagId") FROM stdin;
\.


--
-- Data for Name: insights_by_period; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.insights_by_period (id, "metaId", type, value, "periodUnit", "periodStart") FROM stdin;
\.


--
-- Data for Name: insights_metadata; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.insights_metadata ("metaId", "workflowId", "projectId", "workflowName", "projectName") FROM stdin;
\.


--
-- Data for Name: insights_raw; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.insights_raw (id, "metaId", type, value, "timestamp") FROM stdin;
\.


--
-- Data for Name: installed_nodes; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.installed_nodes (name, type, "latestVersion", package) FROM stdin;
\.


--
-- Data for Name: installed_packages; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.installed_packages ("packageName", "installedVersion", "authorName", "authorEmail", "createdAt", "updatedAt") FROM stdin;
\.


--
-- Data for Name: invalid_auth_token; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.invalid_auth_token (token, "expiresAt") FROM stdin;
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjBmOTM3MjU2LWIwMjctNGQxZS05MThkLTY2MjE5MWY2MTA4MCIsImhhc2giOiI3NXBEbm1QSC9DIiwiYnJvd3NlcklkIjoiS3VqTysyRU5FL3JJaElLQVYzQUdMbUtHa0ptTjl5emJrQ09wUkR6YTdPQT0iLCJ1c2VkTWZhIjpmYWxzZSwiaWF0IjoxNzU0MzMxOTA1LCJleHAiOjE3NTQ5MzY3MDV9.OmeValq_vNdzWQfJhrNcQbtys8D1EhqJmw6AmTyBpoA	2025-08-11 18:25:05+00
\.


--
-- Data for Name: migrations; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.migrations (id, "timestamp", name) FROM stdin;
1	1587669153312	InitialMigration1587669153312
2	1589476000887	WebhookModel1589476000887
3	1594828256133	CreateIndexStoppedAt1594828256133
4	1607431743768	MakeStoppedAtNullable1607431743768
5	1611144599516	AddWebhookId1611144599516
6	1617270242566	CreateTagEntity1617270242566
7	1620824779533	UniqueWorkflowNames1620824779533
8	1626176912946	AddwaitTill1626176912946
9	1630419189837	UpdateWorkflowCredentials1630419189837
10	1644422880309	AddExecutionEntityIndexes1644422880309
11	1646834195327	IncreaseTypeVarcharLimit1646834195327
12	1646992772331	CreateUserManagement1646992772331
13	1648740597343	LowerCaseUserEmail1648740597343
14	1652254514002	CommunityNodes1652254514002
15	1652367743993	AddUserSettings1652367743993
16	1652905585850	AddAPIKeyColumn1652905585850
17	1654090467022	IntroducePinData1654090467022
18	1658932090381	AddNodeIds1658932090381
19	1659902242948	AddJsonKeyPinData1659902242948
20	1660062385367	CreateCredentialsUserRole1660062385367
21	1663755770893	CreateWorkflowsEditorRole1663755770893
22	1664196174001	WorkflowStatistics1664196174001
23	1665484192212	CreateCredentialUsageTable1665484192212
24	1665754637025	RemoveCredentialUsageTable1665754637025
25	1669739707126	AddWorkflowVersionIdColumn1669739707126
26	1669823906995	AddTriggerCountColumn1669823906995
27	1671535397530	MessageEventBusDestinations1671535397530
28	1671726148421	RemoveWorkflowDataLoadedFlag1671726148421
29	1673268682475	DeleteExecutionsWithWorkflows1673268682475
30	1674138566000	AddStatusToExecutions1674138566000
31	1674509946020	CreateLdapEntities1674509946020
32	1675940580449	PurgeInvalidWorkflowConnections1675940580449
33	1676996103000	MigrateExecutionStatus1676996103000
34	1677236854063	UpdateRunningExecutionStatus1677236854063
35	1677501636754	CreateVariables1677501636754
36	1679416281778	CreateExecutionMetadataTable1679416281778
37	1681134145996	AddUserActivatedProperty1681134145996
38	1681134145997	RemoveSkipOwnerSetup1681134145997
39	1690000000000	MigrateIntegerKeysToString1690000000000
40	1690000000020	SeparateExecutionData1690000000020
41	1690000000030	RemoveResetPasswordColumns1690000000030
42	1690000000030	AddMfaColumns1690000000030
43	1690787606731	AddMissingPrimaryKeyOnExecutionData1690787606731
44	1691088862123	CreateWorkflowNameIndex1691088862123
45	1692967111175	CreateWorkflowHistoryTable1692967111175
46	1693491613982	ExecutionSoftDelete1693491613982
47	1693554410387	DisallowOrphanExecutions1693554410387
48	1694091729095	MigrateToTimestampTz1694091729095
49	1695128658538	AddWorkflowMetadata1695128658538
50	1695829275184	ModifyWorkflowHistoryNodesAndConnections1695829275184
51	1700571993961	AddGlobalAdminRole1700571993961
52	1705429061930	DropRoleMapping1705429061930
53	1711018413374	RemoveFailedExecutionStatus1711018413374
54	1711390882123	MoveSshKeysToDatabase1711390882123
55	1712044305787	RemoveNodesAccess1712044305787
56	1714133768519	CreateProject1714133768519
57	1714133768521	MakeExecutionStatusNonNullable1714133768521
58	1717498465931	AddActivatedAtUserSetting1717498465931
59	1720101653148	AddConstraintToExecutionMetadata1720101653148
60	1721377157740	FixExecutionMetadataSequence1721377157740
61	1723627610222	CreateInvalidAuthTokenTable1723627610222
62	1723796243146	RefactorExecutionIndices1723796243146
63	1724753530828	CreateAnnotationTables1724753530828
64	1724951148974	AddApiKeysTable1724951148974
65	1726606152711	CreateProcessedDataTable1726606152711
66	1727427440136	SeparateExecutionCreationFromStart1727427440136
67	1728659839644	AddMissingPrimaryKeyOnAnnotationTagMapping1728659839644
68	1729607673464	UpdateProcessedDataValueColumnToText1729607673464
69	1729607673469	AddProjectIcons1729607673469
70	1730386903556	CreateTestDefinitionTable1730386903556
71	1731404028106	AddDescriptionToTestDefinition1731404028106
72	1731582748663	MigrateTestDefinitionKeyToString1731582748663
73	1732271325258	CreateTestMetricTable1732271325258
74	1732549866705	CreateTestRun1732549866705
75	1733133775640	AddMockedNodesColumnToTestDefinition1733133775640
76	1734479635324	AddManagedColumnToCredentialsTable1734479635324
77	1736172058779	AddStatsColumnsToTestRun1736172058779
78	1736947513045	CreateTestCaseExecutionTable1736947513045
79	1737715421462	AddErrorColumnsToTestRuns1737715421462
80	1738709609940	CreateFolderTable1738709609940
81	1739549398681	CreateAnalyticsTables1739549398681
82	1740445074052	UpdateParentFolderIdColumn1740445074052
83	1741167584277	RenameAnalyticsToInsights1741167584277
84	1742918400000	AddScopesColumnToApiKeys1742918400000
85	1745322634000	ClearEvaluation1745322634000
86	1745587087521	AddWorkflowStatisticsRootCount1745587087521
87	1745934666076	AddWorkflowArchivedColumn1745934666076
88	1745934666077	DropRoleTable1745934666077
89	1747824239000	AddProjectDescriptionColumn1747824239000
90	1750252139166	AddLastActiveAtColumnToUser1750252139166
91	1752669793000	AddInputsOutputsToTestCaseExecution1752669793000
\.


--
-- Data for Name: processed_data; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.processed_data ("workflowId", context, "createdAt", "updatedAt", value) FROM stdin;
\.


--
-- Data for Name: project; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.project (id, name, type, "createdAt", "updatedAt", icon, description) FROM stdin;
cYDW64LvTU6JhUNR	ranger admin <ranger@ranger.local>	personal	2025-08-04 18:24:44.426+00	2025-08-07 19:25:15.017+00	\N	\N
\.


--
-- Data for Name: project_relation; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.project_relation ("projectId", "userId", role, "createdAt", "updatedAt") FROM stdin;
cYDW64LvTU6JhUNR	0f937256-b027-4d1e-918d-662191f61080	project:personalOwner	2025-08-04 18:24:44.426+00	2025-08-04 18:24:44.426+00
\.


--
-- Data for Name: settings; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.settings (key, value, "loadOnStartup") FROM stdin;
ui.banners.dismissed	["V1"]	t
features.ldap	{"loginEnabled":false,"loginLabel":"","connectionUrl":"","allowUnauthorizedCerts":false,"connectionSecurity":"none","connectionPort":389,"baseDn":"","bindingAdminDn":"","bindingAdminPassword":"","firstNameAttribute":"","lastNameAttribute":"","emailAttribute":"","loginIdAttribute":"","ldapIdAttribute":"","userFilter":"","synchronizationEnabled":false,"synchronizationInterval":60,"searchPageSize":0,"searchTimeout":60}	t
features.oidc	{"clientId":"","clientSecret":"","discoveryEndpoint":"","loginEnabled":false}	t
userManagement.authenticationMethod	email	t
features.sourceControl.sshKeys	{"encryptedPrivateKey":"U2FsdGVkX1/EXGMKklut87c+OwxdtP1TnrbIM5U129CFgCsRZ68I6rsa28MtAsp9ME3zNXCI4KuXCtXWwrXIAKHM2USzc6iI+XXXM3Lik6NNQfDzxENHA5zjovHk4ZWUgcwrpZF7yMUSjAkjwYqDV+HebnBQLEN2XRdp5o8pCP8aKy5WaEYQqFLsrx+geCqcPyAloEIyPYUzFewKSiXg+7J1pLCinGgMaCfAyhaJiNnF/IAzWbt48lQCC6ybVzk5/MhB/Gu1b/ZugX5MmJSX6K3mukjkcKLv7N8HC6qi6MV1fgtV/kITJ58rrJdzXVW2CJdu0SuTArT1PyNmqegod+Aq4cfRJ70146YkY+vuLhRBxDnDCk+48Et/Brjr8UR9tgZvKRinYQB/gmBbOXwsEkWXFM1sudO2dz74mDQkb1KzNVOU7HiKEaYUWhybJCmiPPEhkai3Cu6vIBqFoQZhOQjgIQ7F6vjw8V4uBrtirCVqee4hJStXqeLRmAZG1pSRH9jRQB7bKQoR58j4XIqaHpSjzsDAByOoyN0SHjZDRt3d2Rpu9aXvrqxOJZhese36","publicKey":"ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIFodmM3v0NC4yzHipKvXKRMYqK6Hr3Kx3QXL/dgtDwyu n8n deploy key"}	t
features.sourceControl	{"branchName":"main","keyGeneratorType":"ed25519"}	t
userManagement.isInstanceOwnerSetUp	true	t
\.


--
-- Data for Name: shared_credentials; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.shared_credentials ("credentialsId", "projectId", role, "createdAt", "updatedAt") FROM stdin;
IG6IHbnpp3S6McZf	cYDW64LvTU6JhUNR	credential:owner	2025-08-07 19:29:56.364+00	2025-08-07 19:29:56.364+00
cucV1OHveuszsqcf	cYDW64LvTU6JhUNR	credential:owner	2025-08-07 19:56:56.537+00	2025-08-07 19:56:56.537+00
Wntvg0re72jrk9lu	cYDW64LvTU6JhUNR	credential:owner	2025-08-07 19:58:22.659+00	2025-08-07 19:58:22.659+00
\.


--
-- Data for Name: shared_workflow; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.shared_workflow ("workflowId", "projectId", role, "createdAt", "updatedAt") FROM stdin;
JVZVi78xcQLB83nf	cYDW64LvTU6JhUNR	workflow:owner	2025-08-07 19:53:44.161+00	2025-08-07 19:53:44.161+00
YkOKiC1YLPv50CKE	cYDW64LvTU6JhUNR	workflow:owner	2025-08-07 19:57:35.096+00	2025-08-07 19:57:35.096+00
EvH5kvVirO6NhH3V	cYDW64LvTU6JhUNR	workflow:owner	2025-08-07 19:59:39.688+00	2025-08-07 19:59:39.688+00
nbAb70691HaRUT3P	cYDW64LvTU6JhUNR	workflow:owner	2025-08-07 20:00:40.943+00	2025-08-07 20:00:40.943+00
lDdTnLlZpbSbfsIZ	cYDW64LvTU6JhUNR	workflow:owner	2025-08-07 20:01:24.319+00	2025-08-07 20:01:24.319+00
\.


--
-- Data for Name: tag_entity; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.tag_entity (name, "createdAt", "updatedAt", id) FROM stdin;
\.


--
-- Data for Name: test_case_execution; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.test_case_execution (id, "testRunId", "executionId", status, "runAt", "completedAt", "errorCode", "errorDetails", metrics, "createdAt", "updatedAt", inputs, outputs) FROM stdin;
\.


--
-- Data for Name: test_run; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.test_run (id, "workflowId", status, "errorCode", "errorDetails", "runAt", "completedAt", metrics, "createdAt", "updatedAt") FROM stdin;
\.


--
-- Data for Name: user; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public."user" (id, email, "firstName", "lastName", password, "personalizationAnswers", "createdAt", "updatedAt", settings, disabled, "mfaEnabled", "mfaSecret", "mfaRecoveryCodes", role, "lastActiveAt") FROM stdin;
0f937256-b027-4d1e-918d-662191f61080	ranger@ranger.local	ranger	admin	$2a$10$YXP.ma8MCPp5ecu74H.RMuMNLTGBWlSNmhfXDpHK.QdJ0ZH0I/KJy	{"version":"v4","personalization_survey_submitted_at":"2025-08-04T18:25:07.282Z","personalization_survey_n8n_version":"1.106.0"}	2025-08-04 18:24:43.916+00	2025-08-08 04:01:13.965+00	{"userActivated":false,"easyAIWorkflowOnboarded":true}	f	f	\N	\N	global:owner	2025-08-08
\.


--
-- Data for Name: user_api_keys; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.user_api_keys (id, "userId", label, "apiKey", "createdAt", "updatedAt", scopes) FROM stdin;
\.


--
-- Data for Name: variables; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.variables (key, type, value, id) FROM stdin;
\.


--
-- Data for Name: webhook_entity; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.webhook_entity ("webhookPath", method, node, "webhookId", "pathLength", "workflowId") FROM stdin;
\.


--
-- Data for Name: workflow_entity; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.workflow_entity (name, active, nodes, connections, "createdAt", "updatedAt", settings, "staticData", "pinData", "versionId", "triggerCount", id, meta, "parentFolderId", "isArchived") FROM stdin;
ghosts_intent	f	[{"parameters":{"httpMethod":"POST","path":"ghosts-intent","responseMode":"responseNode","options":{"rawBody":true}},"type":"n8n-nodes-base.webhook","typeVersion":2,"position":[0,0],"id":"a048cbe6-16d4-4368-a265-b91a6318e8a9","name":"Webhook","webhookId":"2cb4b190-bf9f-4704-abed-6ec67e6422f8"},{"parameters":{"options":{}},"type":"n8n-nodes-base.respondToWebhook","typeVersion":1.2,"position":[592,0],"id":"2768b30d-919a-4d88-a884-379ad56f3819","name":"Respond to Webhook"},{"parameters":{"modelId":{"__rl":true,"value":"={{ $json.body.model }}","mode":"id"},"messages":{"values":[{"content":"={{ $json.body.messages[0].content }}"}]},"options":{}},"type":"@n8n/n8n-nodes-langchain.openAi","typeVersion":1.8,"position":[304,0],"id":"f30901ad-0829-47cf-8cc1-698f95e7f38c","name":"Ollama Text","credentials":{"openAiApi":{"id":"cucV1OHveuszsqcf","name":"OpenAi account"}}}]	{"Webhook":{"main":[[{"node":"Ollama Text","type":"main","index":0}]]},"Ollama Text":{"main":[[{"node":"Respond to Webhook","type":"main","index":0}]]}}	2025-08-07 19:57:35.096+00	2025-08-07 19:57:35.096+00	{"executionOrder":"v1"}	\N	{}	fb88cd5a-4973-4049-a04d-d7f385a07622	0	YkOKiC1YLPv50CKE	{"templateCredsSetupCompleted":true}	\N	f
ranger	f	[{"parameters":{"content":"## MCP Clients\\n","height":760,"width":480,"color":6},"id":"c19f3479-47bf-4b4c-97f5-9ab983491f65","name":"Sticky Note1","type":"n8n-nodes-base.stickyNote","position":[832,464],"typeVersion":1},{"parameters":{"options":{"systemMessage":"=You are Ranger, an advanced AI agent integrated into the CMU SEI GHOSTS ecosystem. Your expertise is in the design, behavior modeling, and operational control of Non-Player Characters (NPCs) within GHOSTS, specifically tailored for cybersecurity training, simulation, and realistic cyber exercise environments. Your responsibilities include analyzing agent activities, suggesting behavioral enhancements, troubleshooting NPC interactions, and providing detailed insights on agent performance and architectural best practices."}},"id":"265d2de6-09aa-4257-8106-061ad1aefe68","name":"Ranger AI Agent","type":"@n8n/n8n-nodes-langchain.agent","position":[768,224],"typeVersion":1.8},{"parameters":{"description":"=Use the tool to think about something. It will not obtain new information or change the database, but just append the thought to the log. Use it when complex reasoning or some cache memory is needed."},"type":"@n8n/n8n-nodes-langchain.toolThink","typeVersion":1,"position":[1520,560],"id":"9aedf55b-35d9-4637-bdd8-0a7ab8545655","name":"Think"},{"parameters":{"content":"## Reconsider...\\n","height":300,"width":340,"color":7},"id":"40fd82bd-1ed6-4f5e-95f8-b6aa85400c3e","name":"Sticky Note2","type":"n8n-nodes-base.stickyNote","position":[1472,464],"typeVersion":1},{"parameters":{"content":"## Local Models\\n","height":240,"width":360,"color":3},"id":"b1a33a2b-ad1f-4130-af1c-4fcf51f43658","name":"Sticky Note3","type":"n8n-nodes-base.stickyNote","position":[224,464],"typeVersion":1},{"parameters":{"mode":"retrieve-as-tool","toolName":"ghosts","toolDescription":"ghosts documentation","qdrantCollection":{"__rl":true,"value":"ghosts","mode":"list","cachedResultName":"ghosts"},"options":{}},"type":"@n8n/n8n-nodes-langchain.vectorStoreQdrant","typeVersion":1.1,"position":[1504,928],"id":"211083d6-4da2-4988-94cc-312457b427e1","name":"Qdrant Vector Store"},{"parameters":{"content":"## RAG of Documentation","height":420,"width":340,"color":7},"id":"fc5e5258-bfe2-4b06-a6cf-c7fa6617a69a","name":"Sticky Note4","type":"n8n-nodes-base.stickyNote","position":[1472,800],"typeVersion":1},{"parameters":{"httpMethod":"POST","path":"ranger/v0","responseMode":"responseNode","options":{}},"type":"n8n-nodes-base.webhook","typeVersion":2,"position":[48,224],"id":"75671bcd-d51d-4f06-b1cd-4fb7b5ea4e29","name":"Webhook","webhookId":"49e1820f-6dc0-4f87-92c0-33dc9c68d12b"},{"parameters":{"assignments":{"assignments":[{"id":"62dc7747-1dfa-4cdd-9085-6944d8a3d683","name":"sessionId","value":"={{ $json.body.sessionId }}","type":"string"},{"id":"118a2f98-82f6-4aba-b52b-4b9bc6b160f9","name":"chatInput","value":"={{ $json.body.messages[($json.body.messages.length - 1)].content }}","type":"string"},{"id":"ffe21bcd-5bba-4459-9547-aad0e097b251","name":"model","value":"={{ $json.body.model }}","type":"string"}]},"options":{}},"type":"n8n-nodes-base.set","typeVersion":3.4,"position":[272,224],"id":"257295ab-2e62-4315-bf62-fa8d12ce5e5c","name":"Edit Fields"},{"parameters":{"assignments":{"assignments":[{"id":"9a7293c4-c113-46de-847f-b7a66b0d8add","name":"sessionId","value":"=SessionId","type":"string"},{"id":"11d34333-b273-4f49-b8a5-3217cad50285","name":"output","value":"={{ $json.output }}","type":"string"}]},"options":{}},"type":"n8n-nodes-base.set","typeVersion":3.4,"position":[1264,224],"id":"2dbd8334-a507-4155-a4ce-f1972d70674b","name":"FinalFieldSetter"},{"parameters":{"content":"## SessionId Memory\\n","height":240,"width":180,"color":5},"id":"4b9b4ff1-4329-4ba6-aad6-1fbcdd03ad2f","name":"Sticky Note6","type":"n8n-nodes-base.stickyNote","position":[608,464],"typeVersion":1},{"parameters":{"sseEndpoint":"http://host.docker.internal:8501/sse"},"type":"@n8n/n8n-nodes-langchain.mcpClientTool","typeVersion":1,"position":[928,560],"id":"2669c722-3476-4d46-bf80-6806a0e7ec74","name":"GHOSTS MCP Client"},{"parameters":{"respondWith":"json","responseBody":"={\\n    \\"model\\": \\"{{ $('Webhook').item.json.body.model }}\\",\\n    \\"created_at\\": \\"{{ new Date().toISOString() }}\\",\\n    \\"message\\": {\\n        \\"role\\": \\"assistant\\",\\n        \\"content\\": \\"{{ $json.output.toString().replaceAll('\\"', \\"'\\").replace(/\\\\r?\\\\n/g, '\\\\\\\\n') }}\\"\\n    },\\n    \\"done_reason\\": \\"stop\\",\\n    \\"done\\": true,\\n    \\"total_duration\\": 4259340292,\\n    \\"load_duration\\": 34489792,\\n    \\"prompt_eval_count\\": 30,\\n    \\"prompt_eval_duration\\": 255913750,\\n    \\"eval_count\\": 344,\\n    \\"eval_duration\\": 3968224209\\n}","options":{}},"type":"n8n-nodes-base.respondToWebhook","typeVersion":1.2,"position":[1600,224],"id":"96cad8b9-5bbf-4aa5-8329-46e6228389a2","name":"Respond to Webhook"},{"parameters":{"sessionIdType":"customKey","sessionKey":"={{ $('Edit Fields').item.json.sessionId }}"},"type":"@n8n/n8n-nodes-langchain.memoryBufferWindow","typeVersion":1.3,"position":[640,560],"id":"e17d7a48-894d-45b1-bb20-c129488135ce","name":"Simple Memory"},{"parameters":{"model":"llama3.2:3b","options":{}},"type":"@n8n/n8n-nodes-langchain.lmChatOllama","typeVersion":1,"position":[464,544],"id":"d12f5300-0d76-4178-8b23-3399d494adf1","name":"Ollama Chat Model","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{"model":"nomic-embed-text:latest"},"type":"@n8n/n8n-nodes-langchain.embeddingsOllama","typeVersion":1,"position":[1600,1088],"id":"dd93e907-efd7-42eb-a742-a667ab849f5a","name":"Embeddings Ollama"}]	{"Think":{"ai_tool":[[{"node":"Ranger AI Agent","type":"ai_tool","index":0}]]},"Qdrant Vector Store":{"ai_tool":[[{"node":"Ranger AI Agent","type":"ai_tool","index":0}]]},"Ranger AI Agent":{"main":[[{"node":"FinalFieldSetter","type":"main","index":0}]]},"Webhook":{"main":[[{"node":"Edit Fields","type":"main","index":0}]]},"Edit Fields":{"main":[[{"node":"Ranger AI Agent","type":"main","index":0}]]},"FinalFieldSetter":{"main":[[{"node":"Respond to Webhook","type":"main","index":0}]]},"GHOSTS MCP Client":{"ai_tool":[[{"node":"Ranger AI Agent","type":"ai_tool","index":0}]]},"Simple Memory":{"ai_memory":[[{"node":"Ranger AI Agent","type":"ai_memory","index":0}]]},"Ollama Chat Model":{"ai_languageModel":[[{"node":"Ranger AI Agent","type":"ai_languageModel","index":0}]]},"Embeddings Ollama":{"ai_embedding":[[{"node":"Qdrant Vector Store","type":"ai_embedding","index":0}]]}}	2025-08-07 19:53:44.161+00	2025-08-07 19:57:42.83+00	{"executionOrder":"v1"}	\N	{}	2df59d07-ba65-4e87-b170-c1b28c976b07	0	JVZVi78xcQLB83nf	\N	\N	f
workroles	f	[{"parameters":{"options":{}},"type":"@n8n/n8n-nodes-langchain.chatTrigger","typeVersion":1.1,"position":[-160,800],"id":"3173cd56-6a30-476c-9e2f-7057e6b773bb","name":"When chat message received","webhookId":"c384160e-db9f-4d99-9d31-91e44647e6ad"},{"parameters":{"promptType":"define","text":"={{ $json.body.query }}","options":{"systemMessage":"You are a helpful assistant providing information about the NICE work roles and how other content is related to those roles. You can only answer questions in regard to the information you have about the work roles. \\n\\nNEVER make up a work role. The only possible matches are from the work_roles_tool, in there you will find:\\n\\nOG-WRL-001\\nOG-WRL-002\\nOG-WRL-003\\nOG-WRL-004\\nOG-WRL-005\\nOG-WRL-006\\nOG-WRL-007\\nOG-WRL-008\\nOG-WRL-009\\nOG-WRL-010\\nOG-WRL-011\\nOG-WRL-012\\nOG-WRL-013\\nOG-WRL-014\\nOG-WRL-015\\nOG-WRL-016\\nDD-WRL-001\\nDD-WRL-002\\nDD-WRL-003\\nDD-WRL-004\\nDD-WRL-005\\nDD-WRL-006\\nDD-WRL-007\\nDD-WRL-008\\nDD-WRL-009\\nIO-WRL-001\\nIO-WRL-002\\nIO-WRL-003\\nIO-WRL-004\\nIO-WRL-005\\nIO-WRL-006\\nIO-WRL-007\\nPD-WRL-001\\nPD-WRL-002\\nPD-WRL-003\\nPD-WRL-004\\nPD-WRL-005\\nPD-WRL-006\\nPD-WRL-007\\nIN-WRL-001\\nIN-WRL-002\\n\\nIf you come up with one that isn't in this list, then remove it. Do not claim insufficient information or say it might be. Just list it and provide the likelihood. No jabber.\\n\\nIt is perfectly OK to have more than one match.\\n\\nYou should also provide a likelihood of match. 1.0 being a entirely perfect match, 0.0 being not a match. Any number below 1 should start with a 0. Don't include anything that is below 0.4.\\n\\nIMPORTANT: You must return a single valid JSON array. Never return multiple arrays or multiple JSON blocks.\\n\\nTHE OUTPUT FORMAT MUST LOOK LIKE THIS EXAMPLE:\\n\\n[\\n  {\\n    \\"WORKROLEID\\": \\"OG-WRL-001\\",\\n    \\"description\\": \\"Some description here\\",\\n    \\"reasoning\\": \\"Why this role fits\\",\\n    \\"likelihood\\": 0.87\\n  },\\n  {\\n    \\"WORKROLEID\\": \\"PD-WRL-002\\",\\n    \\"description\\": \\"Another description\\",\\n    \\"reasoning\\": \\"Another justification\\",\\n    \\"likelihood\\": 0.91\\n  }\\n]\\n\\nLastly, \\n\\n[\\n  { \\"WORKROLEID\\": \\"OG-WRL-001\\", ... }\\n]\\n[\\n  { \\"WORKROLEID\\": \\"OG-WRL-002\\", ... }\\n]\\n❌ THIS IS INVALID. NEVER DO THIS. \\n\\nIf you return more than one array, your entire answer will be rejected.\\nThe system will parse your response with JSON.parse() — it will fail if you return multiple top-level arrays.\\nReturn exactly one array. No text. No prefix. No suffix."}},"type":"@n8n/n8n-nodes-langchain.agent","typeVersion":1.9,"position":[320,864],"id":"e074e9f5-8e60-4a0f-9a36-1e5a1cf9bbd4","name":"AI Agent"},{"parameters":{"model":"mistral:7b","options":{}},"type":"@n8n/n8n-nodes-langchain.lmChatOllama","typeVersion":1,"position":[144,1104],"id":"1ba49836-c10b-462f-86de-214120091eac","name":"Ollama Chat Model","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{},"type":"@n8n/n8n-nodes-langchain.memoryBufferWindow","typeVersion":1.3,"position":[320,1104],"id":"8c124ebf-0d31-4848-8b9f-ff67f5d0d2c1","name":"Simple Memory"},{"parameters":{"qdrantCollection":{"__rl":true,"value":"workroles","mode":"list","cachedResultName":"workroles"},"options":{}},"type":"@n8n/n8n-nodes-langchain.vectorStoreQdrant","typeVersion":1.1,"position":[512,1232],"id":"2180e47e-3369-4d2a-8c80-cab6ce707db7","name":"Qdrant Vector Store1","credentials":{"qdrantApi":{"id":"Wntvg0re72jrk9lu","name":"QdrantApi account"}}},{"parameters":{"model":"nomic-embed-text:latest"},"type":"@n8n/n8n-nodes-langchain.embeddingsOllama","typeVersion":1,"position":[528,1392],"id":"0eaab4f3-94de-4b2f-89ed-e3429147a0ca","name":"Embeddings Ollama1","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{"options":{}},"id":"52adeaf9-d249-4829-ae15-8aff69e923aa","name":"Default Data Loader","type":"@n8n/n8n-nodes-langchain.documentDefaultDataLoader","position":[560,304],"typeVersion":1},{"parameters":{"options":{}},"id":"b92fd6b6-d6ff-4940-9ffd-b5bf78604743","name":"Recursive Character Text Splitter","type":"@n8n/n8n-nodes-langchain.textSplitterRecursiveCharacterTextSplitter","position":[592,448],"typeVersion":1},{"parameters":{"mode":"insert","qdrantCollection":{"__rl":true,"value":"workroles","mode":"list","cachedResultName":"workroles"},"options":{}},"type":"@n8n/n8n-nodes-langchain.vectorStoreQdrant","typeVersion":1.1,"position":[384,128],"id":"bae65666-c99e-4d5e-be52-94158ceb47ff","name":"Qdrant Vector Store","credentials":{"qdrantApi":{"id":"Wntvg0re72jrk9lu","name":"QdrantApi account"}}},{"parameters":{"model":"nomic-embed-text:latest"},"type":"@n8n/n8n-nodes-langchain.embeddingsOllama","typeVersion":1,"position":[320,400],"id":"c6fce3f9-3e62-49be-968b-421de470a577","name":"Embeddings Ollama","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{"formTitle":"Upload file","formFields":{"values":[{"fieldLabel":"File","fieldType":"file"}]},"options":{}},"type":"n8n-nodes-base.formTrigger","typeVersion":2.2,"position":[80,128],"id":"f1c7de07-c15d-4279-8761-05e45e9e0976","name":"On form submission","webhookId":"c2bc8a95-b393-4828-b1e4-224244df1afa"},{"parameters":{"model":"llama3.2:3b","options":{}},"type":"@n8n/n8n-nodes-langchain.lmOllama","typeVersion":1,"position":[880,1232],"id":"f17ad8ba-5239-4b83-8c6d-57b507dd4048","name":"Ollama Model","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{"name":"work_roles_tool","description":"Use this tool to get information about NICE Work roles."},"id":"8d18f0e1-8350-40a5-afa1-8b15f762a4f7","name":"Vector Store Tool","type":"@n8n/n8n-nodes-langchain.toolVectorStore","position":[592,1040],"typeVersion":1},{"parameters":{"content":"## Load documents into DB","height":620,"width":1040},"type":"n8n-nodes-base.stickyNote","typeVersion":1,"position":[0,0],"id":"44ad1a35-c0d8-43d3-909a-bc19e85132cd","name":"Sticky Note"},{"parameters":{"content":"## Query documents","height":800,"width":1040,"color":4},"type":"n8n-nodes-base.stickyNote","typeVersion":1,"position":[0,752],"id":"ea10bb1a-55d9-4e83-af51-9b28eb7c1d86","name":"Sticky Note1"},{"parameters":{"httpMethod":"POST","path":"work-roles","responseMode":"lastNode","options":{}},"type":"n8n-nodes-base.webhook","typeVersion":2,"position":[-128,1040],"id":"5d18d807-68fa-4a47-8473-f9438f248603","name":"Webhook","webhookId":"c574c363-b114-442f-a270-8ddf1839d260"},{"parameters":{"assignments":{"assignments":[{"id":"f2fc7988-3fb3-4cd0-b496-fee49d2d60cf","name":"body.query","value":"={{ $json.chatInput ?? $json.body.query }}","type":"string"},{"id":"bf5633ee-9ee3-48e2-a988-06b26ebd414a","name":"sessionId","value":"={{ $json.sessionId ?? $json.headers['postman-token'] }}","type":"string"}]},"options":{}},"type":"n8n-nodes-base.set","typeVersion":3.4,"position":[80,864],"id":"2b790e58-f236-4ca3-a35e-05655b3d3f46","name":"Edit Fields"}]	{"When chat message received":{"main":[[{"node":"Edit Fields","type":"main","index":0}]]},"Ollama Chat Model":{"ai_languageModel":[[{"node":"AI Agent","type":"ai_languageModel","index":0}]]},"Simple Memory":{"ai_memory":[[{"node":"AI Agent","type":"ai_memory","index":0}]]},"Qdrant Vector Store1":{"ai_vectorStore":[[{"node":"Vector Store Tool","type":"ai_vectorStore","index":0}]]},"Embeddings Ollama1":{"ai_embedding":[[{"node":"Qdrant Vector Store1","type":"ai_embedding","index":0}]]},"Default Data Loader":{"ai_document":[[{"node":"Qdrant Vector Store","type":"ai_document","index":0}]]},"Recursive Character Text Splitter":{"ai_textSplitter":[[{"node":"Default Data Loader","type":"ai_textSplitter","index":0}]]},"Embeddings Ollama":{"ai_embedding":[[{"node":"Qdrant Vector Store","type":"ai_embedding","index":0}]]},"On form submission":{"main":[[{"node":"Qdrant Vector Store","type":"main","index":0}]]},"Ollama Model":{"ai_languageModel":[[{"node":"Vector Store Tool","type":"ai_languageModel","index":0}]]},"Vector Store Tool":{"ai_tool":[[{"node":"AI Agent","type":"ai_tool","index":0}]]},"Webhook":{"main":[[{"node":"Edit Fields","type":"main","index":0}]]},"Edit Fields":{"main":[[{"node":"AI Agent","type":"main","index":0}]]}}	2025-08-07 19:59:39.688+00	2025-08-07 19:59:39.688+00	{"executionOrder":"v1"}	\N	{}	ec74241c-4c16-4303-8d46-9b151f5c4b3b	0	EvH5kvVirO6NhH3V	{"templateCredsSetupCompleted":true}	\N	f
course_development	f	[{"parameters":{"httpMethod":"POST","path":"courses","responseMode":"responseNode","options":{}},"type":"n8n-nodes-base.webhook","typeVersion":2,"position":[-208,128],"id":"54f5bec0-17e2-4cdc-955c-a204dbc8d601","name":"Webhook","webhookId":"f4a9b9bb-c355-4d36-93e5-fd25b9f43318"},{"parameters":{"options":{"systemMessage":"Embrace your role as a lesson plan or lesson module creator.\\n\\nBased on the information provided, you must produce appropriate sub-topics of things to be taught, knowledge checks and tasks that a user should be able to do for the topic, hands-on lab concepts, and similar for the lesson.\\n\\nDo NOT return anything but what I have asked. NO lengthy explanations into your reasoning please.\\n"}},"type":"@n8n/n8n-nodes-langchain.agent","typeVersion":1.9,"position":[240,128],"id":"08c45f78-0c41-4c56-874f-1a28a6585118","name":"AI Agent"},{"parameters":{},"type":"@n8n/n8n-nodes-langchain.memoryBufferWindow","typeVersion":1.3,"position":[352,544],"id":"3f433102-b258-428e-b9d0-e4cca47e5607","name":"Simple Memory"},{"parameters":{"options":{}},"type":"n8n-nodes-base.respondToWebhook","typeVersion":1.2,"position":[608,128],"id":"5b8ef01b-1b2a-46b2-afa0-6a1b1acd639c","name":"Respond to Webhook"},{"parameters":{"assignments":{"assignments":[{"id":"62dc7747-1dfa-4cdd-9085-6944d8a3d683","name":"sessionId","value":"={{ $json.body.sessionId }}","type":"string"},{"id":"118a2f98-82f6-4aba-b52b-4b9bc6b160f9","name":"chatInput","value":"={{ $json.body.messages[($json.body.messages.length - 1)].content }}","type":"string"},{"id":"ffe21bcd-5bba-4459-9547-aad0e097b251","name":"model","value":"={{ $json.body.model }}","type":"string"}]},"options":{}},"type":"n8n-nodes-base.set","typeVersion":3.4,"position":[0,0],"id":"87ce7145-a63b-4635-bd25-a20f44081e77","name":"Edit Fields"},{"parameters":{"model":"mistral:7b","options":{}},"type":"@n8n/n8n-nodes-langchain.lmChatOllama","typeVersion":1,"position":[128,464],"id":"fcb78a6c-629c-46c8-b78c-17d00b723996","name":"Ollama Chat Model","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}}]	{"Webhook":{"main":[[{"node":"Edit Fields","type":"main","index":0}]]},"Simple Memory":{"ai_memory":[[{"node":"AI Agent","type":"ai_memory","index":0}]]},"AI Agent":{"main":[[{"node":"Respond to Webhook","type":"main","index":0}]]},"Edit Fields":{"main":[[{"node":"AI Agent","type":"main","index":0}]]},"Ollama Chat Model":{"ai_languageModel":[[{"node":"AI Agent","type":"ai_languageModel","index":0}]]}}	2025-08-07 20:00:40.943+00	2025-08-07 20:00:40.943+00	{"executionOrder":"v1"}	\N	{}	9873e2f3-425c-4d5e-a62e-8655c83fa0ba	0	nbAb70691HaRUT3P	{"templateCredsSetupCompleted":true}	\N	f
attack_t_codes	f	[{"parameters":{"options":{}},"type":"@n8n/n8n-nodes-langchain.chatTrigger","typeVersion":1.1,"position":[-160,800],"id":"c137f73d-2ab7-4048-b0bc-c1aa113e3f4d","name":"When chat message received","webhookId":"f03329f6-c8fd-43d3-a64b-62fb0a53da87"},{"parameters":{"promptType":"define","text":"={{ $json.body.query }}","options":{"systemMessage":"You are a helpful assistant providing information about the MITRE ATTACK T-Codes and how other content is related to those tactics. You can only answer questions in regard to the information you have about the work roles. \\n\\nNEVER make up a t-code. The only possible matches are available with the t_codes_tool. Do not claim insufficient information or say it might be. Just list it and provide the likelihood. No jabber.\\n\\nThere is likely to be more than one match for any piece of content — this is fine.\\n\\nYou should also provide a likelihood of match. 1.0 being a entirely perfect match, 0.0 being not a match. Any number below 1 should start with a 0. Don't include anything that is below 0.4.\\n\\nIMPORTANT: You must return a single valid JSON array. Never return multiple arrays or multiple JSON blocks.\\n\\nTHE OUTPUT FORMAT MUST LOOK LIKE THIS EXAMPLE:\\n\\n[\\n  {\\n    \\"ID\\": \\"T1005\\",\\n    \\"Name\\": \\"Data from Local System\\",\\n    \\"Description\\": \\"Adversaries may search local system sources, such as file systems, configuration files, local databases, or virtual machine files, to find files of interest and sensitive data prior to Exfiltration.\\",\\n    \\"Reasoning\\": \\"Why this role fits\\",\\n    \\"Likelihood\\": 0.87\\n  },\\n  {\\n    \\"ID\\": \\"T1113\\",\\n    \\"Name\\": \\"Screen Capture\\",\\n    \\"Description\\": \\"Adversaries may attempt to take screen captures of the desktop to gather information over the course of an operation. Screen capturing functionality may be included as a feature of a remote access tool used in post-compromise operations. Taking a screenshot is also typically possible through native utilities or API calls, such as <code>CopyFromScreen</code>, <code>xwd</code>, or <code>screencapture</code>.(Citation: CopyFromScreen .NET)(Citation: Antiquated Mac Malware)\\",\\n    \\"Reasoning\\": \\"Why this role fits\\",\\n    \\"Likelihood\\": 0.5\\n  }\\n]\\n\\nLastly, if you return more than one array, your entire answer will be rejected.\\nThe system will parse your response with JSON.parse() — it will fail if you return multiple top-level arrays.\\nReturn exactly one array. No text. No prefix. No suffix."}},"type":"@n8n/n8n-nodes-langchain.agent","typeVersion":1.9,"position":[320,864],"id":"b89cfa04-f51f-4ef9-9049-02383a96eb91","name":"AI Agent"},{"parameters":{"model":"mistral:7b","options":{}},"type":"@n8n/n8n-nodes-langchain.lmChatOllama","typeVersion":1,"position":[144,1104],"id":"526b4b5e-caf8-4acd-a3d9-105ddd5224a1","name":"Ollama Chat Model","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{},"type":"@n8n/n8n-nodes-langchain.memoryBufferWindow","typeVersion":1.3,"position":[320,1104],"id":"e1c4e3e8-aef7-47b6-8e85-60b7e4bd66fc","name":"Simple Memory"},{"parameters":{"qdrantCollection":{"__rl":true,"value":"attack_t_codes","mode":"id"},"options":{}},"type":"@n8n/n8n-nodes-langchain.vectorStoreQdrant","typeVersion":1.1,"position":[512,1232],"id":"30e9f533-38f8-42a9-a209-e24b66a8a4b9","name":"Qdrant Vector Store1","credentials":{"qdrantApi":{"id":"Wntvg0re72jrk9lu","name":"QdrantApi account"}}},{"parameters":{"model":"nomic-embed-text:latest"},"type":"@n8n/n8n-nodes-langchain.embeddingsOllama","typeVersion":1,"position":[528,1392],"id":"473a95e5-a819-4871-b1c0-9f9372b55cf2","name":"Embeddings Ollama1","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{"options":{}},"id":"0371b511-d982-42d8-a561-871f5d11774b","name":"Default Data Loader","type":"@n8n/n8n-nodes-langchain.documentDefaultDataLoader","position":[560,304],"typeVersion":1},{"parameters":{"options":{}},"id":"7933afc3-e264-4d21-82e4-ca1dcebce8e0","name":"Recursive Character Text Splitter","type":"@n8n/n8n-nodes-langchain.textSplitterRecursiveCharacterTextSplitter","position":[592,448],"typeVersion":1},{"parameters":{"mode":"insert","qdrantCollection":{"__rl":true,"value":"attack_t_codes","mode":"id"},"options":{}},"type":"@n8n/n8n-nodes-langchain.vectorStoreQdrant","typeVersion":1.1,"position":[384,128],"id":"b2370f20-2df0-4ad3-b2cd-dbfa746c36ce","name":"Qdrant Vector Store","credentials":{"qdrantApi":{"id":"Wntvg0re72jrk9lu","name":"QdrantApi account"}}},{"parameters":{"model":"nomic-embed-text:latest"},"type":"@n8n/n8n-nodes-langchain.embeddingsOllama","typeVersion":1,"position":[320,400],"id":"28248baf-6ed9-40ee-92e9-de3afda77e1c","name":"Embeddings Ollama","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{"formTitle":"Upload file","formFields":{"values":[{"fieldLabel":"File","fieldType":"file"}]},"options":{}},"type":"n8n-nodes-base.formTrigger","typeVersion":2.2,"position":[80,128],"id":"0513dacd-279e-435a-9ed8-1c35a94c4285","name":"On form submission","webhookId":"5d79cd66-2344-47d2-98c0-af51c0c476f1"},{"parameters":{"model":"llama3.2:3b","options":{}},"type":"@n8n/n8n-nodes-langchain.lmOllama","typeVersion":1,"position":[880,1232],"id":"2f6f30f3-5766-4c3e-a53b-a38449b85294","name":"Ollama Model","credentials":{"ollamaApi":{"id":"IG6IHbnpp3S6McZf","name":"Ollama account"}}},{"parameters":{"name":"t_codes_tool","description":"Use this tool to get information about MITRE ATTACK T-codes. This database contains their specifications."},"id":"10c5d18c-e19c-48db-828a-104e4b81674c","name":"Vector Store Tool","type":"@n8n/n8n-nodes-langchain.toolVectorStore","position":[592,1040],"typeVersion":1},{"parameters":{"content":"## Load documents into DB","height":620,"width":1040},"type":"n8n-nodes-base.stickyNote","typeVersion":1,"position":[0,0],"id":"87afedf1-2db0-45c7-ad6d-f3d777bee04d","name":"Sticky Note"},{"parameters":{"content":"## Query documents","height":800,"width":1040,"color":4},"type":"n8n-nodes-base.stickyNote","typeVersion":1,"position":[0,752],"id":"c193f533-ff62-4f1f-864b-50eaec62fe64","name":"Sticky Note1"},{"parameters":{"httpMethod":"POST","path":"t-codes","responseMode":"lastNode","options":{}},"type":"n8n-nodes-base.webhook","typeVersion":2,"position":[-128,1040],"id":"7643afc3-2ccc-42b6-9528-3ac716aae312","name":"Webhook","webhookId":"3a13ba4d-5c98-4d0f-8be0-55d56c13a6ce"},{"parameters":{"assignments":{"assignments":[{"id":"f2fc7988-3fb3-4cd0-b496-fee49d2d60cf","name":"body.query","value":"={{ $json.chatInput ?? $json.body.query ?? $json.body.messages[$json.body.messages.length - 1].content }}\\n","type":"string"},{"id":"bf5633ee-9ee3-48e2-a988-06b26ebd414a","name":"sessionId","value":"={{ $json.sessionId ?? $json.body.sessionId ?? $json.headers['postman-token'] }}","type":"string"}]},"options":{}},"type":"n8n-nodes-base.set","typeVersion":3.4,"position":[80,864],"id":"4eaa8db3-4de1-4c38-9d7d-f00e643a76f4","name":"Edit Fields"}]	{"When chat message received":{"main":[[{"node":"Edit Fields","type":"main","index":0}]]},"Ollama Chat Model":{"ai_languageModel":[[{"node":"AI Agent","type":"ai_languageModel","index":0}]]},"Simple Memory":{"ai_memory":[[{"node":"AI Agent","type":"ai_memory","index":0}]]},"Qdrant Vector Store1":{"ai_vectorStore":[[{"node":"Vector Store Tool","type":"ai_vectorStore","index":0}]]},"Embeddings Ollama1":{"ai_embedding":[[{"node":"Qdrant Vector Store1","type":"ai_embedding","index":0}]]},"Default Data Loader":{"ai_document":[[{"node":"Qdrant Vector Store","type":"ai_document","index":0}]]},"Recursive Character Text Splitter":{"ai_textSplitter":[[{"node":"Default Data Loader","type":"ai_textSplitter","index":0}]]},"Embeddings Ollama":{"ai_embedding":[[{"node":"Qdrant Vector Store","type":"ai_embedding","index":0}]]},"On form submission":{"main":[[{"node":"Qdrant Vector Store","type":"main","index":0}]]},"Ollama Model":{"ai_languageModel":[[{"node":"Vector Store Tool","type":"ai_languageModel","index":0}]]},"Vector Store Tool":{"ai_tool":[[{"node":"AI Agent","type":"ai_tool","index":0}]]},"Webhook":{"main":[[{"node":"Edit Fields","type":"main","index":0}]]},"Edit Fields":{"main":[[{"node":"AI Agent","type":"main","index":0}]]}}	2025-08-07 20:01:24.319+00	2025-08-07 20:01:24.319+00	{"executionOrder":"v1"}	\N	{}	e5814bac-ff48-43b4-96b3-6533c70bdd4d	0	lDdTnLlZpbSbfsIZ	{"templateCredsSetupCompleted":true}	\N	f
\.


--
-- Data for Name: workflow_history; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.workflow_history ("versionId", "workflowId", authors, "createdAt", "updatedAt", nodes, connections) FROM stdin;
\.


--
-- Data for Name: workflow_statistics; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.workflow_statistics (count, "latestEvent", name, "workflowId", "rootCount") FROM stdin;
\.


--
-- Data for Name: workflows_tags; Type: TABLE DATA; Schema: public; Owner: n8n
--

COPY public.workflows_tags ("workflowId", "tagId") FROM stdin;
\.


--
-- Name: auth_provider_sync_history_id_seq; Type: SEQUENCE SET; Schema: public; Owner: n8n
--

SELECT pg_catalog.setval('public.auth_provider_sync_history_id_seq', 1, false);


--
-- Name: execution_annotations_id_seq; Type: SEQUENCE SET; Schema: public; Owner: n8n
--

SELECT pg_catalog.setval('public.execution_annotations_id_seq', 1, false);


--
-- Name: execution_entity_id_seq; Type: SEQUENCE SET; Schema: public; Owner: n8n
--

SELECT pg_catalog.setval('public.execution_entity_id_seq', 1, false);


--
-- Name: execution_metadata_temp_id_seq; Type: SEQUENCE SET; Schema: public; Owner: n8n
--

SELECT pg_catalog.setval('public.execution_metadata_temp_id_seq', 1, false);


--
-- Name: insights_by_period_id_seq; Type: SEQUENCE SET; Schema: public; Owner: n8n
--

SELECT pg_catalog.setval('public.insights_by_period_id_seq', 1, false);


--
-- Name: insights_metadata_metaId_seq; Type: SEQUENCE SET; Schema: public; Owner: n8n
--

SELECT pg_catalog.setval('public."insights_metadata_metaId_seq"', 1, false);


--
-- Name: insights_raw_id_seq; Type: SEQUENCE SET; Schema: public; Owner: n8n
--

SELECT pg_catalog.setval('public.insights_raw_id_seq', 1, false);


--
-- Name: migrations_id_seq; Type: SEQUENCE SET; Schema: public; Owner: n8n
--

SELECT pg_catalog.setval('public.migrations_id_seq', 91, true);


--
-- Name: test_run PK_011c050f566e9db509a0fadb9b9; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.test_run
    ADD CONSTRAINT "PK_011c050f566e9db509a0fadb9b9" PRIMARY KEY (id);


--
-- Name: installed_packages PK_08cc9197c39b028c1e9beca225940576fd1a5804; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.installed_packages
    ADD CONSTRAINT "PK_08cc9197c39b028c1e9beca225940576fd1a5804" PRIMARY KEY ("packageName");


--
-- Name: execution_metadata PK_17a0b6284f8d626aae88e1c16e4; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_metadata
    ADD CONSTRAINT "PK_17a0b6284f8d626aae88e1c16e4" PRIMARY KEY (id);


--
-- Name: project_relation PK_1caaa312a5d7184a003be0f0cb6; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.project_relation
    ADD CONSTRAINT "PK_1caaa312a5d7184a003be0f0cb6" PRIMARY KEY ("projectId", "userId");


--
-- Name: folder_tag PK_27e4e00852f6b06a925a4d83a3e; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.folder_tag
    ADD CONSTRAINT "PK_27e4e00852f6b06a925a4d83a3e" PRIMARY KEY ("folderId", "tagId");


--
-- Name: project PK_4d68b1358bb5b766d3e78f32f57; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.project
    ADD CONSTRAINT "PK_4d68b1358bb5b766d3e78f32f57" PRIMARY KEY (id);


--
-- Name: invalid_auth_token PK_5779069b7235b256d91f7af1a15; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.invalid_auth_token
    ADD CONSTRAINT "PK_5779069b7235b256d91f7af1a15" PRIMARY KEY (token);


--
-- Name: shared_workflow PK_5ba87620386b847201c9531c58f; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.shared_workflow
    ADD CONSTRAINT "PK_5ba87620386b847201c9531c58f" PRIMARY KEY ("workflowId", "projectId");


--
-- Name: folder PK_6278a41a706740c94c02e288df8; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.folder
    ADD CONSTRAINT "PK_6278a41a706740c94c02e288df8" PRIMARY KEY (id);


--
-- Name: annotation_tag_entity PK_69dfa041592c30bbc0d4b84aa00; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.annotation_tag_entity
    ADD CONSTRAINT "PK_69dfa041592c30bbc0d4b84aa00" PRIMARY KEY (id);


--
-- Name: execution_annotations PK_7afcf93ffa20c4252869a7c6a23; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_annotations
    ADD CONSTRAINT "PK_7afcf93ffa20c4252869a7c6a23" PRIMARY KEY (id);


--
-- Name: migrations PK_8c82d7f526340ab734260ea46be; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.migrations
    ADD CONSTRAINT "PK_8c82d7f526340ab734260ea46be" PRIMARY KEY (id);


--
-- Name: installed_nodes PK_8ebd28194e4f792f96b5933423fc439df97d9689; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.installed_nodes
    ADD CONSTRAINT "PK_8ebd28194e4f792f96b5933423fc439df97d9689" PRIMARY KEY (name);


--
-- Name: shared_credentials PK_8ef3a59796a228913f251779cff; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.shared_credentials
    ADD CONSTRAINT "PK_8ef3a59796a228913f251779cff" PRIMARY KEY ("credentialsId", "projectId");


--
-- Name: test_case_execution PK_90c121f77a78a6580e94b794bce; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.test_case_execution
    ADD CONSTRAINT "PK_90c121f77a78a6580e94b794bce" PRIMARY KEY (id);


--
-- Name: user_api_keys PK_978fa5caa3468f463dac9d92e69; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.user_api_keys
    ADD CONSTRAINT "PK_978fa5caa3468f463dac9d92e69" PRIMARY KEY (id);


--
-- Name: execution_annotation_tags PK_979ec03d31294cca484be65d11f; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_annotation_tags
    ADD CONSTRAINT "PK_979ec03d31294cca484be65d11f" PRIMARY KEY ("annotationId", "tagId");


--
-- Name: webhook_entity PK_b21ace2e13596ccd87dc9bf4ea6; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.webhook_entity
    ADD CONSTRAINT "PK_b21ace2e13596ccd87dc9bf4ea6" PRIMARY KEY ("webhookPath", method);


--
-- Name: insights_by_period PK_b606942249b90cc39b0265f0575; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.insights_by_period
    ADD CONSTRAINT "PK_b606942249b90cc39b0265f0575" PRIMARY KEY (id);


--
-- Name: workflow_history PK_b6572dd6173e4cd06fe79937b58; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflow_history
    ADD CONSTRAINT "PK_b6572dd6173e4cd06fe79937b58" PRIMARY KEY ("versionId");


--
-- Name: processed_data PK_ca04b9d8dc72de268fe07a65773; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.processed_data
    ADD CONSTRAINT "PK_ca04b9d8dc72de268fe07a65773" PRIMARY KEY ("workflowId", context);


--
-- Name: settings PK_dc0fe14e6d9943f268e7b119f69ab8bd; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.settings
    ADD CONSTRAINT "PK_dc0fe14e6d9943f268e7b119f69ab8bd" PRIMARY KEY (key);


--
-- Name: user PK_ea8f538c94b6e352418254ed6474a81f; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT "PK_ea8f538c94b6e352418254ed6474a81f" PRIMARY KEY (id);


--
-- Name: insights_raw PK_ec15125755151e3a7e00e00014f; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.insights_raw
    ADD CONSTRAINT "PK_ec15125755151e3a7e00e00014f" PRIMARY KEY (id);


--
-- Name: insights_metadata PK_f448a94c35218b6208ce20cf5a1; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.insights_metadata
    ADD CONSTRAINT "PK_f448a94c35218b6208ce20cf5a1" PRIMARY KEY ("metaId");


--
-- Name: user UQ_e12875dfb3b1d92d7d7c5377e2; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public."user"
    ADD CONSTRAINT "UQ_e12875dfb3b1d92d7d7c5377e2" UNIQUE (email);


--
-- Name: auth_identity auth_identity_pkey; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.auth_identity
    ADD CONSTRAINT auth_identity_pkey PRIMARY KEY ("providerId", "providerType");


--
-- Name: auth_provider_sync_history auth_provider_sync_history_pkey; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.auth_provider_sync_history
    ADD CONSTRAINT auth_provider_sync_history_pkey PRIMARY KEY (id);


--
-- Name: credentials_entity credentials_entity_pkey; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.credentials_entity
    ADD CONSTRAINT credentials_entity_pkey PRIMARY KEY (id);


--
-- Name: event_destinations event_destinations_pkey; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.event_destinations
    ADD CONSTRAINT event_destinations_pkey PRIMARY KEY (id);


--
-- Name: execution_data execution_data_pkey; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_data
    ADD CONSTRAINT execution_data_pkey PRIMARY KEY ("executionId");


--
-- Name: execution_entity pk_e3e63bbf986767844bbe1166d4e; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_entity
    ADD CONSTRAINT pk_e3e63bbf986767844bbe1166d4e PRIMARY KEY (id);


--
-- Name: workflow_statistics pk_workflow_statistics; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflow_statistics
    ADD CONSTRAINT pk_workflow_statistics PRIMARY KEY ("workflowId", name);


--
-- Name: workflows_tags pk_workflows_tags; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflows_tags
    ADD CONSTRAINT pk_workflows_tags PRIMARY KEY ("workflowId", "tagId");


--
-- Name: tag_entity tag_entity_pkey; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.tag_entity
    ADD CONSTRAINT tag_entity_pkey PRIMARY KEY (id);


--
-- Name: variables variables_key_key; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.variables
    ADD CONSTRAINT variables_key_key UNIQUE (key);


--
-- Name: variables variables_pkey; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.variables
    ADD CONSTRAINT variables_pkey PRIMARY KEY (id);


--
-- Name: workflow_entity workflow_entity_pkey; Type: CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflow_entity
    ADD CONSTRAINT workflow_entity_pkey PRIMARY KEY (id);


--
-- Name: IDX_14f68deffaf858465715995508; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX "IDX_14f68deffaf858465715995508" ON public.folder USING btree ("projectId", id);


--
-- Name: IDX_1d8ab99d5861c9388d2dc1cf73; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX "IDX_1d8ab99d5861c9388d2dc1cf73" ON public.insights_metadata USING btree ("workflowId");


--
-- Name: IDX_1e31657f5fe46816c34be7c1b4; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_1e31657f5fe46816c34be7c1b4" ON public.workflow_history USING btree ("workflowId");


--
-- Name: IDX_1ef35bac35d20bdae979d917a3; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX "IDX_1ef35bac35d20bdae979d917a3" ON public.user_api_keys USING btree ("apiKey");


--
-- Name: IDX_5f0643f6717905a05164090dde; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_5f0643f6717905a05164090dde" ON public.project_relation USING btree ("userId");


--
-- Name: IDX_60b6a84299eeb3f671dfec7693; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX "IDX_60b6a84299eeb3f671dfec7693" ON public.insights_by_period USING btree ("periodStart", type, "periodUnit", "metaId");


--
-- Name: IDX_61448d56d61802b5dfde5cdb00; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_61448d56d61802b5dfde5cdb00" ON public.project_relation USING btree ("projectId");


--
-- Name: IDX_63d7bbae72c767cf162d459fcc; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX "IDX_63d7bbae72c767cf162d459fcc" ON public.user_api_keys USING btree ("userId", label);


--
-- Name: IDX_8e4b4774db42f1e6dda3452b2a; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_8e4b4774db42f1e6dda3452b2a" ON public.test_case_execution USING btree ("testRunId");


--
-- Name: IDX_97f863fa83c4786f1956508496; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX "IDX_97f863fa83c4786f1956508496" ON public.execution_annotations USING btree ("executionId");


--
-- Name: IDX_a3697779b366e131b2bbdae297; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_a3697779b366e131b2bbdae297" ON public.execution_annotation_tags USING btree ("tagId");


--
-- Name: IDX_ae51b54c4bb430cf92f48b623f; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX "IDX_ae51b54c4bb430cf92f48b623f" ON public.annotation_tag_entity USING btree (name);


--
-- Name: IDX_c1519757391996eb06064f0e7c; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_c1519757391996eb06064f0e7c" ON public.execution_annotation_tags USING btree ("annotationId");


--
-- Name: IDX_cec8eea3bf49551482ccb4933e; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX "IDX_cec8eea3bf49551482ccb4933e" ON public.execution_metadata USING btree ("executionId", key);


--
-- Name: IDX_d6870d3b6e4c185d33926f423c; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_d6870d3b6e4c185d33926f423c" ON public.test_run USING btree ("workflowId");


--
-- Name: IDX_execution_entity_deletedAt; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_execution_entity_deletedAt" ON public.execution_entity USING btree ("deletedAt");


--
-- Name: IDX_workflow_entity_name; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX "IDX_workflow_entity_name" ON public.workflow_entity USING btree (name);


--
-- Name: idx_07fde106c0b471d8cc80a64fc8; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX idx_07fde106c0b471d8cc80a64fc8 ON public.credentials_entity USING btree (type);


--
-- Name: idx_16f4436789e804e3e1c9eeb240; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX idx_16f4436789e804e3e1c9eeb240 ON public.webhook_entity USING btree ("webhookId", method, "pathLength");


--
-- Name: idx_812eb05f7451ca757fb98444ce; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX idx_812eb05f7451ca757fb98444ce ON public.tag_entity USING btree (name);


--
-- Name: idx_execution_entity_stopped_at_status_deleted_at; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX idx_execution_entity_stopped_at_status_deleted_at ON public.execution_entity USING btree ("stoppedAt", status, "deletedAt") WHERE (("stoppedAt" IS NOT NULL) AND ("deletedAt" IS NULL));


--
-- Name: idx_execution_entity_wait_till_status_deleted_at; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX idx_execution_entity_wait_till_status_deleted_at ON public.execution_entity USING btree ("waitTill", status, "deletedAt") WHERE (("waitTill" IS NOT NULL) AND ("deletedAt" IS NULL));


--
-- Name: idx_execution_entity_workflow_id_started_at; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX idx_execution_entity_workflow_id_started_at ON public.execution_entity USING btree ("workflowId", "startedAt") WHERE (("startedAt" IS NOT NULL) AND ("deletedAt" IS NULL));


--
-- Name: idx_workflows_tags_workflow_id; Type: INDEX; Schema: public; Owner: n8n
--

CREATE INDEX idx_workflows_tags_workflow_id ON public.workflows_tags USING btree ("workflowId");


--
-- Name: pk_credentials_entity_id; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX pk_credentials_entity_id ON public.credentials_entity USING btree (id);


--
-- Name: pk_tag_entity_id; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX pk_tag_entity_id ON public.tag_entity USING btree (id);


--
-- Name: pk_variables_id; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX pk_variables_id ON public.variables USING btree (id);


--
-- Name: pk_workflow_entity_id; Type: INDEX; Schema: public; Owner: n8n
--

CREATE UNIQUE INDEX pk_workflow_entity_id ON public.workflow_entity USING btree (id);


--
-- Name: processed_data FK_06a69a7032c97a763c2c7599464; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.processed_data
    ADD CONSTRAINT "FK_06a69a7032c97a763c2c7599464" FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE CASCADE;


--
-- Name: insights_metadata FK_1d8ab99d5861c9388d2dc1cf733; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.insights_metadata
    ADD CONSTRAINT "FK_1d8ab99d5861c9388d2dc1cf733" FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE SET NULL;


--
-- Name: workflow_history FK_1e31657f5fe46816c34be7c1b4b; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflow_history
    ADD CONSTRAINT "FK_1e31657f5fe46816c34be7c1b4b" FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE CASCADE;


--
-- Name: insights_metadata FK_2375a1eda085adb16b24615b69c; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.insights_metadata
    ADD CONSTRAINT "FK_2375a1eda085adb16b24615b69c" FOREIGN KEY ("projectId") REFERENCES public.project(id) ON DELETE SET NULL;


--
-- Name: execution_metadata FK_31d0b4c93fb85ced26f6005cda3; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_metadata
    ADD CONSTRAINT "FK_31d0b4c93fb85ced26f6005cda3" FOREIGN KEY ("executionId") REFERENCES public.execution_entity(id) ON DELETE CASCADE;


--
-- Name: shared_credentials FK_416f66fc846c7c442970c094ccf; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.shared_credentials
    ADD CONSTRAINT "FK_416f66fc846c7c442970c094ccf" FOREIGN KEY ("credentialsId") REFERENCES public.credentials_entity(id) ON DELETE CASCADE;


--
-- Name: project_relation FK_5f0643f6717905a05164090dde7; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.project_relation
    ADD CONSTRAINT "FK_5f0643f6717905a05164090dde7" FOREIGN KEY ("userId") REFERENCES public."user"(id) ON DELETE CASCADE;


--
-- Name: project_relation FK_61448d56d61802b5dfde5cdb002; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.project_relation
    ADD CONSTRAINT "FK_61448d56d61802b5dfde5cdb002" FOREIGN KEY ("projectId") REFERENCES public.project(id) ON DELETE CASCADE;


--
-- Name: insights_by_period FK_6414cfed98daabbfdd61a1cfbc0; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.insights_by_period
    ADD CONSTRAINT "FK_6414cfed98daabbfdd61a1cfbc0" FOREIGN KEY ("metaId") REFERENCES public.insights_metadata("metaId") ON DELETE CASCADE;


--
-- Name: insights_raw FK_6e2e33741adef2a7c5d66befa4e; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.insights_raw
    ADD CONSTRAINT "FK_6e2e33741adef2a7c5d66befa4e" FOREIGN KEY ("metaId") REFERENCES public.insights_metadata("metaId") ON DELETE CASCADE;


--
-- Name: installed_nodes FK_73f857fc5dce682cef8a99c11dbddbc969618951; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.installed_nodes
    ADD CONSTRAINT "FK_73f857fc5dce682cef8a99c11dbddbc969618951" FOREIGN KEY (package) REFERENCES public.installed_packages("packageName") ON UPDATE CASCADE ON DELETE CASCADE;


--
-- Name: folder FK_804ea52f6729e3940498bd54d78; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.folder
    ADD CONSTRAINT "FK_804ea52f6729e3940498bd54d78" FOREIGN KEY ("parentFolderId") REFERENCES public.folder(id) ON DELETE CASCADE;


--
-- Name: shared_credentials FK_812c2852270da1247756e77f5a4; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.shared_credentials
    ADD CONSTRAINT "FK_812c2852270da1247756e77f5a4" FOREIGN KEY ("projectId") REFERENCES public.project(id) ON DELETE CASCADE;


--
-- Name: test_case_execution FK_8e4b4774db42f1e6dda3452b2af; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.test_case_execution
    ADD CONSTRAINT "FK_8e4b4774db42f1e6dda3452b2af" FOREIGN KEY ("testRunId") REFERENCES public.test_run(id) ON DELETE CASCADE;


--
-- Name: folder_tag FK_94a60854e06f2897b2e0d39edba; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.folder_tag
    ADD CONSTRAINT "FK_94a60854e06f2897b2e0d39edba" FOREIGN KEY ("folderId") REFERENCES public.folder(id) ON DELETE CASCADE;


--
-- Name: execution_annotations FK_97f863fa83c4786f19565084960; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_annotations
    ADD CONSTRAINT "FK_97f863fa83c4786f19565084960" FOREIGN KEY ("executionId") REFERENCES public.execution_entity(id) ON DELETE CASCADE;


--
-- Name: execution_annotation_tags FK_a3697779b366e131b2bbdae2976; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_annotation_tags
    ADD CONSTRAINT "FK_a3697779b366e131b2bbdae2976" FOREIGN KEY ("tagId") REFERENCES public.annotation_tag_entity(id) ON DELETE CASCADE;


--
-- Name: shared_workflow FK_a45ea5f27bcfdc21af9b4188560; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.shared_workflow
    ADD CONSTRAINT "FK_a45ea5f27bcfdc21af9b4188560" FOREIGN KEY ("projectId") REFERENCES public.project(id) ON DELETE CASCADE;


--
-- Name: folder FK_a8260b0b36939c6247f385b8221; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.folder
    ADD CONSTRAINT "FK_a8260b0b36939c6247f385b8221" FOREIGN KEY ("projectId") REFERENCES public.project(id) ON DELETE CASCADE;


--
-- Name: execution_annotation_tags FK_c1519757391996eb06064f0e7c8; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_annotation_tags
    ADD CONSTRAINT "FK_c1519757391996eb06064f0e7c8" FOREIGN KEY ("annotationId") REFERENCES public.execution_annotations(id) ON DELETE CASCADE;


--
-- Name: test_run FK_d6870d3b6e4c185d33926f423c8; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.test_run
    ADD CONSTRAINT "FK_d6870d3b6e4c185d33926f423c8" FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE CASCADE;


--
-- Name: shared_workflow FK_daa206a04983d47d0a9c34649ce; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.shared_workflow
    ADD CONSTRAINT "FK_daa206a04983d47d0a9c34649ce" FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE CASCADE;


--
-- Name: folder_tag FK_dc88164176283de80af47621746; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.folder_tag
    ADD CONSTRAINT "FK_dc88164176283de80af47621746" FOREIGN KEY ("tagId") REFERENCES public.tag_entity(id) ON DELETE CASCADE;


--
-- Name: user_api_keys FK_e131705cbbc8fb589889b02d457; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.user_api_keys
    ADD CONSTRAINT "FK_e131705cbbc8fb589889b02d457" FOREIGN KEY ("userId") REFERENCES public."user"(id) ON DELETE CASCADE;


--
-- Name: test_case_execution FK_e48965fac35d0f5b9e7f51d8c44; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.test_case_execution
    ADD CONSTRAINT "FK_e48965fac35d0f5b9e7f51d8c44" FOREIGN KEY ("executionId") REFERENCES public.execution_entity(id) ON DELETE SET NULL;


--
-- Name: auth_identity auth_identity_userId_fkey; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.auth_identity
    ADD CONSTRAINT "auth_identity_userId_fkey" FOREIGN KEY ("userId") REFERENCES public."user"(id);


--
-- Name: execution_data execution_data_fk; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_data
    ADD CONSTRAINT execution_data_fk FOREIGN KEY ("executionId") REFERENCES public.execution_entity(id) ON DELETE CASCADE;


--
-- Name: execution_entity fk_execution_entity_workflow_id; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.execution_entity
    ADD CONSTRAINT fk_execution_entity_workflow_id FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE CASCADE;


--
-- Name: webhook_entity fk_webhook_entity_workflow_id; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.webhook_entity
    ADD CONSTRAINT fk_webhook_entity_workflow_id FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE CASCADE;


--
-- Name: workflow_entity fk_workflow_parent_folder; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflow_entity
    ADD CONSTRAINT fk_workflow_parent_folder FOREIGN KEY ("parentFolderId") REFERENCES public.folder(id) ON DELETE CASCADE;


--
-- Name: workflow_statistics fk_workflow_statistics_workflow_id; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflow_statistics
    ADD CONSTRAINT fk_workflow_statistics_workflow_id FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE CASCADE;


--
-- Name: workflows_tags fk_workflows_tags_tag_id; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflows_tags
    ADD CONSTRAINT fk_workflows_tags_tag_id FOREIGN KEY ("tagId") REFERENCES public.tag_entity(id) ON DELETE CASCADE;


--
-- Name: workflows_tags fk_workflows_tags_workflow_id; Type: FK CONSTRAINT; Schema: public; Owner: n8n
--

ALTER TABLE ONLY public.workflows_tags
    ADD CONSTRAINT fk_workflows_tags_workflow_id FOREIGN KEY ("workflowId") REFERENCES public.workflow_entity(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

--
-- Database "postgres" dump
--

\connect postgres

--
-- PostgreSQL database dump
--

-- Dumped from database version 15.13 (Debian 15.13-1.pgdg120+1)
-- Dumped by pg_dump version 15.13 (Debian 15.13-1.pgdg120+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- PostgreSQL database dump complete
--

--
-- PostgreSQL database cluster dump complete
--

