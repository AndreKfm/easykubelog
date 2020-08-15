using EasyLogService.Services.CentralLogService;
using LogEntries;
using System;
using System.Diagnostics;

namespace EasyLogService.Commands
{


    // Interface used to send search command
    public interface ISearchCommand
    {
        void Search(SearchRequest request, Action<KubernetesLogEntry[]> completed);
    }

    
    // Handler for search handler
    internal class SearchCommandHandler : ISearchCommand
    {
        readonly ICentralLogServiceQuery _cacheQuery;
        public SearchCommandHandler(ICentralLogServiceCache cache)
        {
            _cacheQuery = cache;
        }
        public void Search(SearchRequest request, Action<KubernetesLogEntry[]> completed)
        {

            Stopwatch w = Stopwatch.StartNew();
            var result = _cacheQuery.Query(request.Query, request.MaxResults, request.From, request.To);

            completed(result);
            Console.WriteLine($"Queried:{request.Query} - result length: {result.Length} needed: {w.ElapsedMilliseconds} ms");
        }
    }

    public class SearchRequest 
    {
        readonly public string Query;
        readonly public int MaxResults;
        readonly public DateTimeOffset From;
        readonly public DateTimeOffset To;
        public SearchRequest(string query, int maxResults, DateTimeOffset from = default, DateTimeOffset to = default)
        {
            Query = query;
            From = from;
            To = to;
            MaxResults = maxResults;
        }

    }

}
