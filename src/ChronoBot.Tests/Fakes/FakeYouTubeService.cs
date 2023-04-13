using System.Collections.Generic;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Requests;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using System;
using System.Net;
using Google;
using System.Net.Http;
using TwitchLib.PubSub.Models.Responses;

namespace ChronoBot.Tests.Fakes
{
    public class FakeYouTubeService : YouTubeService
    {
        public FakeYouTubeService(Initializer initializer) : base(initializer)
        {
        }

        public override FakeSearchResource Search => new(this);
    }

    public class FakeSearchResource : SearchResource
    {
        private readonly IClientService _service;

        public FakeSearchResource(IClientService service) : base(service)
        {
            _service = service;
        }
        
        public override ListRequest List(Repeatable<string> part)
        {
            return new FakeListRequest(_service, part);
        }

        public class FakeListRequest : ListRequest, IClientServiceRequest<FakeSearchListResponse>
        {
            private readonly IClientService _service;
            /// <summary>Constructs a new List request.</summary>
            public FakeListRequest(IClientService service, Repeatable<string> part) : base(service, part)
            {
                _service = service;
            }
            
            public override string MethodName => "list";
            public override string HttpMethod => "GET";
            public override string RestPath => "youtube/v3/search";
            
            public Task<FakeSearchListResponse> ExecuteAsync()
            {
                return Task.FromResult(_service.DeserializeResponse<FakeSearchListResponse>(new HttpResponseMessage(HttpStatusCode.OK)).ConfigureAwait(false).GetAwaiter().GetResult());
            }

            public Task<FakeSearchListResponse> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public FakeSearchListResponse Execute()
            {
                throw new NotImplementedException();
            }
        }
    }

    public class FakeSearchListResponse : SearchListResponse
    {
        public IList<FakeSearchResult> Items { get; set; }
        public override string Kind { get; set; }
    }

    public class FakeSearchResult : SearchResult
    {
        public FakeSearchResult() { }
        public override ResourceId Id { get; set; }
        public override SearchResultSnippet Snippet { get; set; }
        public override string Kind { get; set; }
        public override string ETag { get; set; }
    }

    public partial class FakeClientServiceRequest<TResponse> : ClientServiceRequest<TResponse>
    {
        public FakeClientServiceRequest(IClientService service) : base(service)
        {
        }

        public override string MethodName { get; }
        public override string RestPath { get; }
        public override string HttpMethod { get; }
    }
}
