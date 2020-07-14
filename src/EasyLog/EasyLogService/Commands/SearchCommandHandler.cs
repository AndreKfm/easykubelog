using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyLogService.Commands
{

    public class SearchRequest : IRequest<bool>
    {
        readonly public string Query;
        public SearchRequest(string query)
        {
            Query = query;
        }

    }


    public class SearchCommandHandler : IRequestHandler<SearchRequest, bool>
    {

        public Task<bool> Handle(SearchRequest request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"#### {request.Query}");
            return Task.FromResult(true);
        }

    }

}
