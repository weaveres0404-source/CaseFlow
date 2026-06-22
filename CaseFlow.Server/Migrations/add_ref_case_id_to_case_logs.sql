-- Migration: add ref_case_id to case_logs
-- Run once on production DB

ALTER TABLE case_logs
    ADD COLUMN IF NOT EXISTS ref_case_id INTEGER NULL
        REFERENCES cases(case_id) ON DELETE SET NULL;

COMMENT ON COLUMN case_logs.ref_case_id IS '引用的歷史案件 ID（可空）';
