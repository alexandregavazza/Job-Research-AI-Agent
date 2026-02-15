public interface IJobSource
{
    Task<IEnumerable<JobPosting>> SearchAsync(string keyword);
}
