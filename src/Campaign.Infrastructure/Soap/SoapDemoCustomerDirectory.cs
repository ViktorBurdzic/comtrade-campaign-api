using System.Text;
using System.Xml.Linq;
using Campaign.Application.Common;
using Campaign.Application.Customers;
using Microsoft.Extensions.Logging;

namespace Campaign.Infrastructure.Soap;

// adapter for the legacy SOAP customer directory given in the task (FindPerson operation).
// builds the SOAP 1.1 envelope by hand and parses the response with LINQ to XML.
public sealed class SoapDemoCustomerDirectory : ICustomerDirectory
{
    private const string Namespace = "http://tempuri.org";
    private const string SoapAction = "http://tempuri.org/SOAP.Demo.FindPerson";

    private readonly HttpClient _http;
    private readonly ILogger<SoapDemoCustomerDirectory> _logger;

    public SoapDemoCustomerDirectory(HttpClient http, ILogger<SoapDemoCustomerDirectory> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<CustomerDto?> FindPersonAsync(int id, CancellationToken ct = default)
    {
        // schema is elementFormDefault="qualified", so id must also be in the tempuri namespace
        var envelope =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                "xmlns:tem=\"http://tempuri.org\">" +
                "<soap:Body>" +
                    "<tem:FindPerson>" +
                        $"<tem:id>{id}</tem:id>" +
                    "</tem:FindPerson>" +
                "</soap:Body>" +
            "</soap:Envelope>";

        using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
        };
        // SOAPAction must be a quoted string for SOAP 1.1
        request.Headers.Add("SOAPAction", $"\"{SoapAction}\"");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Customer directory call failed for customer {CustomerId}", id);
            throw new ExternalServiceException("The customer directory is currently unavailable.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        // a SOAP fault for a missing person comes back as HTTP 500 with a <faultstring>;
        // treat that as "not found" rather than a transport error
        if (!response.IsSuccessStatusCode)
        {
            if (body.Contains("faultstring", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Customer {CustomerId} not found (SOAP fault).", id);
                return null;
            }

            _logger.LogWarning("Customer directory returned HTTP {StatusCode} for customer {CustomerId}. Body: {Body}",
                (int)response.StatusCode, id, body);
            throw new ExternalServiceException($"The customer directory returned HTTP {(int)response.StatusCode}.");
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer directory returned non-XML for customer {CustomerId}", id);
            throw new ExternalServiceException("The customer directory returned an unreadable response.", ex);
        }

        var result = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "FindPersonResult");
        if (result is null || !result.HasElements)
            return null;

        string? Field(string localName) =>
            result.Descendants().FirstOrDefault(x => x.Name.LocalName == localName)?.Value;

        var name = Field("Name");
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // City/State live inside the Home address element (see WSDL Person -> Home -> Address)
        return new CustomerDto(
            Id: id,
            Name: name,
            Ssn: Field("SSN"),
            DateOfBirth: Field("DOB"),
            HomeCity: Field("City"),
            HomeState: Field("State"));
    }
}