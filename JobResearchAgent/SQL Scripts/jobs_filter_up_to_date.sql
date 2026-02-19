SELECT title, company, location, url, createdat
FROM public.jobs
WHERE createdat >= CURRENT_DATE
  AND createdat < CURRENT_DATE + TIME '18:15';
