-- Migration: V005 — 修正 cases.case_type CHECK constraint
-- 原 constraint 限制為中文 label，改為英文 enum value

ALTER TABLE cases DROP CONSTRAINT IF EXISTS cases_case_type_check;

ALTER TABLE cases
  ADD CONSTRAINT cases_case_type_check
  CHECK (case_type IN ('REPAIR', 'EVALUATION', 'MAINTENANCE', 'UHD', 'INQUIRY'));
