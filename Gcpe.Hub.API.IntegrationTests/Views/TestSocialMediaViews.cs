﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Gcpe.Hub.API.IntegrationTests.Views
{
    public class TestSocialMediaViews: IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly CustomWebApplicationFactory<Startup> _factory;
        public readonly HttpClient _client;
        public StringContent testPost = TestData.CreateSocialMediaPost(url: "http://facebook.com/post/123", sortOrder: 0);

        public TestSocialMediaViews(CustomWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private async Task<Guid> _PostSocialMediaPost()
        {
            var createResponse = await _client.PostAsync("/api/socialmediaposts", testPost);
            var createBody = await createResponse.Content.ReadAsStringAsync();
            var createdPost = JsonConvert.DeserializeObject<Models.SocialMediaPost>(createBody);
            return createdPost.Id;
        }

        [Fact]
        public async Task List_EndpointShouldReturnSuccessAndCorrectPostsSorted()
        {
            for (var i = 0; i < 5; i++)
            {
                int sortOrder = 5 - i;
                var newPost = TestData.CreateSocialMediaPost("http://facebook.com/post/123", sortOrder);

                var createResponse = await _client.PostAsync("/api/socialmediaposts", newPost);
                createResponse.EnsureSuccessStatusCode();
            }

            var response = await _client.GetAsync("/api/socialmediaposts");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var models = JsonConvert.DeserializeObject<Models.SocialMediaPost[]>(body);

            models.Should().NotBeEmpty();
            models.Should().HaveCount(5);

            for (int i = 0; i < models.Length - 1; i++)
            {
                models[i].SortOrder.Should().BeLessThan(models[i + 1].SortOrder);
            }
        }

        [Fact]
        public async Task Create_EndpointShouldReturnSuccessAndCorrectPost()
        {
            var url = "http://facebook.com/post/123";
            var stringContent = TestData.CreateSocialMediaPost(url, 0);

            var response = await _client.PostAsync("/api/socialmediaposts", stringContent);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var messageResult = JsonConvert.DeserializeObject<Models.SocialMediaPost>(body);

            messageResult.Url.Should().Be(url);
        }

        [Fact]
        public async Task Create_EndpointShouldRequireUrl()
        {
            var invalidPost = new Models.SocialMediaPost
            {
                SortOrder = 0,
            };
            var stringContent = new StringContent(JsonConvert.SerializeObject(invalidPost), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/socialmediaposts", stringContent);
            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task Get_EndpointShouldReturnSuccessAndCorrectPost()
        {
            var id = await _PostSocialMediaPost();

            var response = await _client.GetAsync($"/api/socialmediaposts/{id}");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var postResult = JsonConvert.DeserializeObject<Models.SocialMediaPost>(body);

            postResult.Url.Should().Be(postResult.Url);
            postResult.Id.Should().Be(id.ToString());
        }

        [Fact]
        public async Task Get_EndpointShouldReturnNotFound()
        {
            var response = await _client.GetAsync($"/api/socialmediaposts/{Guid.NewGuid()}");

            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task Put_EndpointShouldReturnSuccessAndCorrectMessage()
        {
            int sortOrder = 10;
            string url = "http://twitter.com/post/123";
            var id = await _PostSocialMediaPost();

            var newPost = TestData.CreateSocialMediaPost(url, sortOrder);
            var response = await _client.PutAsync($"/api/socialmediaposts/{id}", newPost);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var postResult = JsonConvert.DeserializeObject<Models.SocialMediaPost>(body);

            postResult.Url.Should().Be(url);
            postResult.SortOrder.Should().Be(sortOrder);
            postResult.Id.Should().Be(id.ToString());
        }

        [Fact]
        public async Task Put_EndpointShouldRequireUrl()
        {
            var id = await _PostSocialMediaPost();

            var content = new StringContent(JsonConvert.SerializeObject(new { }), Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"/api/socialmediaposts/{id}", content);
            response.IsSuccessStatusCode.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_EndpointShouldDelete()
        {
            var id = await _PostSocialMediaPost();

            var response = await _client.DeleteAsync($"/api/socialmediaposts/{id}");
            response.EnsureSuccessStatusCode();

            var getResponse = await _client.GetAsync($"/api/socialmediaposts/{id}");
            getResponse.IsSuccessStatusCode.Should().BeFalse();
            getResponse.StatusCode.Should().Be(404);

            var getAllResponse = await _client.GetAsync($"/api/socialmediaposts");
            getAllResponse.EnsureSuccessStatusCode();
            var body = await getAllResponse.Content.ReadAsStringAsync();
            var models = JsonConvert.DeserializeObject<Models.SocialMediaPost[]>(body);

            foreach (Models.SocialMediaPost post in models)
            {
                post.Id.Should().NotBe(id);
            }
        }
    }
}
