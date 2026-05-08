using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Baytology.Api.Tests.Infrastructure;

namespace Baytology.Api.Tests.EndpointFlows;

public sealed class ExternalAiEndpointTests(ApiTestWebApplicationFactory factory)
    : IClassFixture<ApiTestWebApplicationFactory>
{
    [Fact]
    public async Task External_ai_proxy_endpoints_return_stubbed_chatbot_and_recommendation_payloads()
    {
        await factory.ResetDatabaseAsync();

        using var buyerClient = factory.CreateAuthenticatedClient(TestSeedData.BuyerUserId, TestSeedData.BuyerEmail, "Buyer");

        var statusResponse = await buyerClient.GetAsync("/api/v1/AiAssistant/status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var statusPayload = await statusResponse.ReadJsonAsync();
        Assert.Equal("Online", statusPayload.GetProperty("overallStatus").GetString());
        Assert.Equal("ok", statusPayload.GetProperty("voiceRecognition").GetProperty("status").GetString());
        Assert.Equal("ok", statusPayload.GetProperty("imageSearch").GetProperty("status").GetString());

        var chatResponse = await buyerClient.PostAsJsonAsync("/api/v1/AiAssistant/chat", new
        {
            session_id = "session-1",
            message = "عايز شقة في القاهرة"
        });
        Assert.Equal(HttpStatusCode.OK, chatResponse.StatusCode);
        var chatPayload = await chatResponse.ReadJsonAsync();
        Assert.Equal("results", chatPayload.GetProperty("type").GetString());
        Assert.Equal(1, chatPayload.GetProperty("properties_count").GetInt32());

        var searchResponse = await buyerClient.PostAsJsonAsync("/api/v1/AiAssistant/search", new
        {
            filters = new
            {
                city = "Cairo"
            }
        });
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchPayload = await searchResponse.ReadJsonAsync();
        Assert.Equal(1, searchPayload.GetProperty("count").GetInt32());

        var recommendationResponse = await buyerClient.GetAsync("/api/v1/AiAssistant/recommend/500?topN=3");
        Assert.Equal(HttpStatusCode.OK, recommendationResponse.StatusCode);
        var recommendationPayload = await recommendationResponse.ReadJsonAsync();
        Assert.Equal(500, recommendationPayload.GetProperty("metadata").GetProperty("query_property_id").GetInt32());

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("session-1"), "session_id");
        var audioContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/webm");
        form.Add(audioContent, "audio", "recording.webm");

        var voiceResponse = await buyerClient.PostAsync("/api/v1/AiAssistant/voice-chat", form);
        Assert.Equal(HttpStatusCode.OK, voiceResponse.StatusCode);
        var voicePayload = await voiceResponse.ReadJsonAsync();
        Assert.Equal("\u0639\u0627\u064a\u0632 \u0634\u0642\u0629 \u0641\u064a \u0627\u0644\u0642\u0627\u0647\u0631\u0629", voicePayload.GetProperty("transcription").GetString());
        Assert.Equal("results", voicePayload.GetProperty("type").GetString());
        Assert.Equal(1, voicePayload.GetProperty("properties_count").GetInt32());

        using var imageForm = new MultipartFormDataContent();
        imageForm.Add(new StringContent("3"), "top_n");
        var imageContent = new ByteArrayContent(new byte[] { 255, 216, 255, 217 });
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        imageForm.Add(imageContent, "image", "property.jpg");

        var imageSearchResponse = await buyerClient.PostAsync("/api/v1/AiAssistant/image-search", imageForm);
        Assert.Equal(HttpStatusCode.OK, imageSearchResponse.StatusCode);
        var imagePayload = await imageSearchResponse.ReadJsonAsync();
        Assert.Equal("visual_similarity_v1", imagePayload.GetProperty("engine").GetString());
        Assert.Equal(1, imagePayload.GetProperty("count").GetInt32());
        Assert.Equal("Image Result", imagePayload.GetProperty("properties")[0].GetProperty("title").GetString());
    }
}
