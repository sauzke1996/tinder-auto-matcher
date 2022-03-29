﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tinder.Exceptions;
using Tinder.Models;

namespace Tinder
{
    public class TinderClient : ITinderClient
    {
        public const string BINGE_WATCHER = "cl_JAyTf";
        public const string GAMING = "cl_7wOvt";

        private readonly HttpClient _httpClient;

        public TinderClient(Guid authToken)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.gotinder.com/"),
            };

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Tinder/7.5.3 (iPhone; iOS 10.3.2; Scale/2.00)");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", authToken.ToString());
        }

        public async Task<IReadOnlyList<Recommendation>?> GetRecommendations(CancellationToken cancellationToken = default)
        {
            var res = await Get<RecommendationResponse>("v2/recs/core", cancellationToken);
            return res.Data.Results;
        }

        public async Task<IReadOnlyList<Recommendation>?> Explore(string category = GAMING, CancellationToken cancellationToken = default)
        {
            var res = await Get<RecommendationResponse>($"v2/explore/recs?locale=en&catalog_id={category}", cancellationToken);
            return res.Data.Results;
        }

        public async Task<TeaserData> GetTeaser(CancellationToken cancellationToken = default)
        {
            var res = await Get<TeaserResponse>("v2/fast-match/teaser", cancellationToken);
            return res.Data;
        }

        public async Task<IReadOnlyList<Teaser>> GetTeasers(CancellationToken cancellationToken = default)
        {
            var res = await Get<TeasersResponse>("v2/fast-match/teasers", cancellationToken);
            return res.Data.Results;
        }

        public async Task<Match> GetMatch(string matchId, CancellationToken cancellationToken = default)
        {
            var res = await Get<MatchResponse>($"matches/" + matchId, cancellationToken);
            return res.Results;
        }

        // TODO: add &message=1 to filter matches you messaged
        public async Task<IReadOnlyList<Match>> GetMatches(int count = 60, CancellationToken cancellationToken = default)
        {
            var res = await Get<MatchesResponse>($"v2/matches?is_tinder_u=false&count={count}", cancellationToken);
            return res.Data.Matches;
        }

        public async Task Ping(Geolocation geolocation, CancellationToken cancellationToken = default)
        {
            await Post<Geolocation, PingResponse>("user/ping", geolocation, cancellationToken);
        }

        public async Task Message(string matchId, string message, CancellationToken cancellationToken = default)
        {
            await Post<MessageRequest, MessageResponse>("user/matches/" + matchId, new MessageRequest { Message = message }, cancellationToken);
        }

        public async Task LikeMessage(string messageId, CancellationToken cancellationToken = default)
        {
            await Post<LikeMessageResponse>($"message/{messageId}/like", cancellationToken);
        }

        public async Task Travel(Geolocation geolocation, CancellationToken cancellationToken = default)
        {
            await Post<Geolocation, TravelResponse>("passport/user/travel", geolocation, cancellationToken);
        }

        public Task<Like> Like(string userId, CancellationToken cancellationToken = default)
        {
            return Get<Like>("like/" + userId, cancellationToken);
        }

        public Task<Like> Superlike(string userId, CancellationToken cancellationToken = default)
        {
            return Post<Like>($"like/{userId}/super", cancellationToken);
        }

        public async Task Pass(string userId, CancellationToken cancellationToken = default)
        {
            await Get<Pass>("pass/" + userId, cancellationToken);
        }

        public async Task Unmatch(string matchId, CancellationToken cancellationToken = default)
        {
            await Delete<UnmatchResponse>("user/matches/" + matchId, cancellationToken);
        }

        public async Task<Profile> GetProfile(CancellationToken cancellationToken = default)
        {
            var res = await Get<ProfileResponse>("v2/profile?locale=en&include=email_settings%2Clikes%2Cuser", cancellationToken);
            return res.Profile;
        }

        public async Task<Meta> GetMetadatas(CancellationToken cancellationToken = default)
        {
            var res = await Get<Meta>("v2/meta", cancellationToken);
            return res;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private Task<TResponse> Get<TResponse>(string requestUri, CancellationToken cancellationToken)
        {
            return Send<TResponse>(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
        }

        private Task<TResponse> Post<TResponse>(string requestUri, CancellationToken cancellationToken)
        {
            return Send<TResponse>(new HttpRequestMessage(HttpMethod.Post, requestUri), cancellationToken);
        }

        private Task<TResponse> Post<TRequest, TResponse>(string requestUri, TRequest payload, CancellationToken cancellationToken)
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var jsonPayload = JsonSerializer.Serialize(payload);
            msg.Content = new StringContent(jsonPayload);
            return Send<TResponse>(msg, cancellationToken);
        }

        private Task<TResponse> Delete<TResponse>(string requestUri, CancellationToken cancellationToken)
        {
            return Send<TResponse>(new HttpRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);
        }

        private async Task<TResponse> Send<TResponse>(HttpRequestMessage msg, CancellationToken cancellationToken)
        {
            var res = await _httpClient.SendAsync(msg, cancellationToken);
            var json = await res.Content.ReadAsStringAsync();

            try
            {
                if (!res.IsSuccessStatusCode)
                {
                    if (res.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new TinderAuthenticationException("Invalid or expired token");
                    }

                    throw new TinderException(json);
                }

                return JsonSerializer.Deserialize<TResponse>(json) 
                    ?? throw new JsonException($"Couldn't deserialize response: ${json}");
            }
            catch (Exception e)
            {
                throw new TinderSerializationException($"Couldn't deserialize response: ${json}", e);
            }
        }
    }
}
