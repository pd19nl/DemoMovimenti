using Microsoft.AspNetCore.Authorization;
using Ordini.Api.Domains.Repositories.Dapper;
using Ordini.Api.Filters;
using Ordini.Api.Helpers.Mapper;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Creazione;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Lettura;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Ordini.Api.Configurations.Endpoints
{
    public static class EndpointsLoad
    {

        //caricamento endpoints
        public static void MapEndpoints(IEndpointRouteBuilder app)
        {
            Endpoint_Root(app);

            RouteGroupBuilder ordiniGroup = app.MapGroup("/api/ordini").RequireAuthorization();
            Endpoint_Ordini_GetById(app, ordiniGroup);
            Endpoint_Ordini_AddOrdine(app, ordiniGroup);

        }

        private static void Endpoint_Root(IEndpointRouteBuilder app)
        {
            //endpoint root
            app.MapGet("/", () => "Ordini Web API avviata!");
        }


        private static void Endpoint_Ordini_GetById(IEndpointRouteBuilder app, RouteGroupBuilder ordiniGroup)
        {
            ordiniGroup.MapGet("/{id}", async (string id, OrdineRepositoryReader ordineRepositoryReader) =>
            {
                Ordine ordineMD = await ordineRepositoryReader.GetOrdineByIdAsync(id);

                if (ordineMD == null)
                    return Results.NotFound();

                OrdineDTO ritorno = OrdineModelToDTO.MapOrdine(ordineMD);
                return Results.Ok(ritorno);

            }).WithName("GetOrdineByID")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "User,Admin" });
        }




        private static void Endpoint_Ordini_AddOrdine(IEndpointRouteBuilder app, RouteGroupBuilder ordiniGroup)
        {
            ordiniGroup.MapPost("/", (AddOrdineDTO nuovoOrdine, IModel channel) =>
            {
                //il dato lo converto nel modello applicativo
                Ordine o = OrdineDTOToModel.MapOrdine(nuovoOrdine);
                Guid IdSaga = Guid.NewGuid();
                o.Id = IdSaga.ToString();

                //creazione evento creato alla base dell'evento richiesto
                OrdineCreatoEvent eventoCreato = OrdineModelToEvent.MapOrdineCreato(o);
                //creazione evento richiesto
                OrdineRichiestoEvent eventoRichiesto = new OrdineRichiestoEvent
                {
                    IdSaga = IdSaga,
                    Ordine = eventoCreato
                };
                //serializzazione messaggio per Service Bus
                string eventoRichiestoSerializzato = JsonSerializer.Serialize(eventoRichiesto);

                //caso RabbitMQ
                var bodyMessaggioRabbitMQ = Encoding.UTF8.GetBytes(eventoRichiestoSerializzato);

                //pubblicazione su exchange degli eventi di tipo TOPIC --> 
                //permette di avere più consumatori per lo stesso evento
                //si usa routing key descrittiva
                channel.ExchangeDeclare("movimenti.saga.eventi.exchange.ordini", ExchangeType.Topic, durable: true);
                channel.BasicPublish(
                    exchange: "movimenti.saga.eventi.exchange.ordini",
                    routingKey: "api.ordine.richiesta",
                    basicProperties: null,
                    body: bodyMessaggioRabbitMQ);


                return Results.Accepted(value: new RispostaAddOrdineDTO() { IdSaga = o.Id });
            })
                .WithName("RichiediNuovoOrdine")
                .AddEndpointFilter<ValidationFilter<AddOrdineDTO>>() //validazione del dato di ingresso
                                                                     // solo per test disabilitato
                                                                     // .RequireAuthorization(new AuthorizeAttribute { Roles = "DataEntry, Admin" });
                                                                     // solo per test abilitato
                .AllowAnonymous();

        }
    }
}
