using EasyLogService.Services;
using System;
using System.Threading.Tasks;

namespace EasyLogService.Commands
{


    public interface ISearchCommand
    {
        void Search(SearchRequest request, Action<KubernetesLogEntry[]> completed);
    }

    

    internal class SearchCommand : ISearchCommand
    {
        ICentralLogServiceQuery _cacheQuery;
        public SearchCommand(ICentralLogServiceCache cache)
        {
            _cacheQuery = cache;
        }
        public void Search(SearchRequest request, Action<KubernetesLogEntry[]> completed)
        {

            var result = _cacheQuery.Query(request.Query);

            completed(result);
            Console.WriteLine($"Queried:{request.Query} - result length: {result.Length}");
        }
    }

    public class SearchRequest 
    {
        readonly public string Query;
        public SearchRequest(string query)
        {
            Query = query;
        }

    }

}
