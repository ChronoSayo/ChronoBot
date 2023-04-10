using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Google.Apis.Discovery;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Requests;

namespace ChronoBot.Tests.Fakes
{
    public class FakeYouTubeService : YouTubeService
    {
        public FakeYouTubeService(FakeSearchResource search)
        {
            Search = search;
        }

        public override FakeSearchResource Search { get; }
    }

    public class FakeSearchResource : SearchResource
    {
        private readonly IClientService _service;
        public FakeSearchResource(IClientService service) : base(service)
        {
            _service = service;
        }
        public override FakeListRequest List(Repeatable<string> part)
        {
            return new FakeListRequest(_service, part);
        }
    }

    public class FakeListRequest : SearchResource.ListRequest
    {
        public FakeListRequest(IClientService service, Repeatable<string> part) : base(service, part)
        {
        }
    }

    public class FakeListRequest<FakeSearchListResponse> : SearchResource.ListRequest
    {
        private readonly Func<FakeClientServiceRequest<FakeSearchListResponse>> _service;
        public FakeListRequest(IClientService service, Repeatable<string> part, Func<FakeClientServiceRequest<FakeSearchListResponse>> service1) : base(service, part)
        {
            _service = service1;
        }
        public override string Q { get; set; }
        public override long? MaxResults { get; set; }
        public async Task<FakeClientServiceRequest<FakeSearchListResponse>> ExecuteAsync()
        {
            return await new Task<FakeClientServiceRequest<FakeSearchListResponse>>(_service);
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

    public class FakeClientServiceRequest<TResponse> : ClientServiceRequest<TResponse>
    {
        public FakeClientServiceRequest(IClientService service) : base(service)
        {
        }

        public override string MethodName { get; }
        public override string RestPath { get; }
        public override string HttpMethod { get; }
    }
}
