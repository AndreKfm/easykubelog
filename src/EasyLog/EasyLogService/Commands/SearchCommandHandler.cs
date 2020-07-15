using System;
using System.Threading.Tasks;

namespace EasyLogService.Commands
{


    public interface ISearchCommand
    {
        Task Search(SearchRequest request, Action<string> completed);
    }

    

    internal class SearchCommand : ISearchCommand
    {
        public async Task Search(SearchRequest request, Action<string> completed)
        {
            await Task.Delay(1000);
            completed(request.Query);
            Console.WriteLine("aha");
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
