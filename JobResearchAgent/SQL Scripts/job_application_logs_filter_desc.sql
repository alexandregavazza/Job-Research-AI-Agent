SELECT job_title, company, location, url, created_at
	FROM public.job_application_logs
	WHERE created_at::date = CURRENT_DATE
	ORDER BY created_at DESC;