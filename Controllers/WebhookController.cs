using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsAppWebhook.Models;
using WhatsAppWebhook.Services;

[ApiController]
[Route("webhooks")]
public class WebhookController : ControllerBase
{
    private const string VerifyToken = "YOUR_VERIFY_TOKEN";
    private readonly EventStore _eventStore;

    public WebhookController(EventStore eventStore)
    {
        _eventStore = eventStore;
    }

    [HttpGet]
    public IActionResult Verify([FromQuery(Name = "hub.mode")] string hub_mode,
                                [FromQuery(Name = "hub.challenge")] string hub_challenge,
                                [FromQuery(Name = "hub.verify_token")] string hub_verify_token)
    {
        if (hub_mode == "subscribe" && hub_verify_token == VerifyToken)
        {
            return Ok(hub_challenge);  // Devuelve el desafío si es válido
        }
        return Forbid();  // Devuelve 403 si el token no coincide
    }

    [HttpPost]
    public async Task<IActionResult> HandleEvent()
    {
        try
        {
            // Leer el cuerpo de la solicitud como texto
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                Console.WriteLine($"JSON recibido: {requestBody}");
                _eventStore.AddEvent(requestBody);

                // Aquí puedes deserializar el JSON si es necesario
                var webhookEvent = JsonConvert.DeserializeObject<WebhookEvent>(requestBody);
                if (webhookEvent == null)
                {
                    Console.WriteLine("El JSON recibido no coincide con el modelo.");
                    return BadRequest("Formato inválido.");
                }

                // Procesar el webhookEvent
                return Ok("Evento procesado correctamente.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando el JSON: {ex.Message}");
            return StatusCode(500, "Error interno del servidor.");
        }
    }
}
