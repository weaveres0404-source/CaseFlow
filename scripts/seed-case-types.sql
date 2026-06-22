SET client_encoding TO 'UTF8';
INSERT INTO case_types (code, label, sort_order) VALUES
  ('REPAIR',      '障礙調查', 10),
  ('EVALUATION',  '工時評估', 20),
  ('MAINTENANCE', '日常維運', 30),
  ('UHD',         'UHD協助', 40),
  ('INQUIRY',     '一般詢問', 50)
ON CONFLICT (code) DO NOTHING;
UPDATE case_types
SET color = CASE code
    WHEN 'REPAIR' THEN 'bg-red-100 text-red-800'
    WHEN 'EVALUATION' THEN 'bg-purple-100 text-purple-800'
    WHEN 'MAINTENANCE' THEN 'bg-blue-100 text-blue-800'
    WHEN 'UHD' THEN 'bg-teal-100 text-teal-800'
    WHEN 'INQUIRY' THEN 'bg-sky-100 text-sky-800'
    ELSE color
END,
updated_at = now()
WHERE code IN ('REPAIR', 'EVALUATION', 'MAINTENANCE', 'UHD', 'INQUIRY');
SELECT * FROM case_types ORDER BY sort_order;
