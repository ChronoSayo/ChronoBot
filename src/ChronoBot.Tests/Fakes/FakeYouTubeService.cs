using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Requests;
using System.Runtime.ExceptionServices;
using System.Threading;

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
            this._service = service;
        }
        
        public override SearchResource.ListRequest List(Repeatable<string> part)
        {
            return new SearchResource.ListRequest(_service, part);
        }

        public class ListRequest : YouTubeBaseServiceRequest<SearchListResponse>
        {
            /// <summary>Constructs a new List request.</summary>
            public ListRequest(IClientService service, Repeatable<string> part) : base(service)
            {
            }

            public override string MethodName { get; }
            public override string RestPath { get; }
            public override string HttpMethod { get; }

            public async void ExecuteAsync()
            {
            }
        }
    }


    public partial class FakeClientServiceRequest<TResponse> : ClientServiceRequest<TResponse>, IClientServiceRequest<TResponse>
    {
        public TResponse Execute()
        {
            return default;
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
