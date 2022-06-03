namespace Tzkt.Api
{
    public interface INormalized
    {
        //TODO Don't forget about OrderBy for all lists
        public string Normalize(string name);
    }
}