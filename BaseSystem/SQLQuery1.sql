SELECT * FROM camera_info;
--ALTER TABLE storage_info ALTER COLUMN path VARCHAR(50);

--DELETE FROM camera_info WHERE cam_id = 10007;
--DELETE FROM process_info WHERE cam_id = 10007;
--DELETE FROM storage_info WHERE cam_id = 10007;
--UPDATE camera_info SET ip = '' WHERE cam_id = 10007;
UPDATE process_info SET core_pid = 123, save_pid = 321 WHERE cam_id = 10007;
SELECT is_conn FROM camera_info WHERE cam_id = 10005;