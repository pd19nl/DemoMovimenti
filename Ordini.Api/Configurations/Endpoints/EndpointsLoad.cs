using Microsoft.AspNetCore.Authorization;
using Ordini.Api.Domains.Repositories.Dapper;
using Ordini.Api.Filters;
using Ordini.Api.Helpers.Mapper;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Creazione;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Lettura;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Modifica;
using Ordini.Contracts;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Models.Ordini;
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
            Endpoint_Ordini_EditOrdine(app, ordiniGroup);
            Endpoint_Ordini_DeleteOrdine(app, ordiniGroup);
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
                Ordine o = OrdineDTOToModel.MapOrdine_Add(nuovoOrdine);
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
                channel.ExchangeDeclare(PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                        ExchangeType.Topic,
                                        durable: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.DURABLE);

                channel.BasicPublish(
                    exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                    routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.CREAZIONE,
                    basicProperties: null,
                    body: bodyMessaggioRabbitMQ);


                return Results.Accepted(value: new RispostaAddEditOrdineDTO() { IdSaga = o.Id });
            })
            .WithName("RichiediNuovoOrdine")
            .AddEndpointFilter<ValidationFilter<AddOrdineDTO>>() //validazione del dato di ingresso
                                                                 // solo per test disabilitato
                                                                 // .RequireAuthorization(new AuthorizeAttribute { Roles = "DataEntry, Admin" });
                                                                 // solo per test abilitato
            .AllowAnonymous();

        }



        private static void Endpoint_Ordini_EditOrdine(IEndpointRouteBuilder app, RouteGroupBuilder ordiniGroup)
        {
            ordiniGroup.MapPut("/", async (EditOrdineDTO editOrdine, IModel channel, OrdineRepositoryReader ordineRepositoryReader) =>
            {
                //lettura ordine origine
                Ordine ordineOrigine = await ordineRepositoryReader.GetOrdineByIdAsync(editOrdine.Id);

                if (ordineOrigine == null)
                    return Results.NotFound();

                //il dato lo converto nel modello applicativo
                Ordine o = OrdineDTOToModel.MapOrdine_Edit(editOrdine);
                Guid IdSaga = Guid.NewGuid();

                //creazione evento modificato alla base dell'evento richiesto
                OrdineModificatoEvent eventoModificato = OrdineModelToEvent.MapOrdineModificato(o);
                eventoModificato.IdSaga = IdSaga;

                //serializzazione messaggio per Service Bus
                string eventoModificatoSerializzato = JsonSerializer.Serialize(eventoModificato);

                //caso RabbitMQ
                var bodyMessaggioRabbitMQ = Encoding.UTF8.GetBytes(eventoModificatoSerializzato);

                //pubblicazione su exchange degli eventi di tipo TOPIC --> 
                //permette di avere più consumatori per lo stesso evento
                //si usa routing key descrittiva                
                channel.ExchangeDeclare(PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                        ExchangeType.Topic,
                                        durable: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.DURABLE);

                channel.BasicPublish(
                    exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                    routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.MODIFICA,
                    basicProperties: null,
                    body: bodyMessaggioRabbitMQ);


                return Results.Accepted(value: new RispostaAddEditOrdineDTO() { IdSaga = o.Id });
            })
            .WithName("RichiediNuovoOrdine")
            .AddEndpointFilter<ValidationFilter<EditOrdineDTO>>() //validazione del dato di ingresso
            .RequireAuthorization(new AuthorizeAttribute { Roles = "DataEntry, Admin" });

        }


        private static void Endpoint_Ordini_DeleteOrdine(IEndpointRouteBuilder app, RouteGroupBuilder ordiniGroup)
        {
            ordiniGroup.MapDelete("/{id:guid}", async (Guid id, IModel channel, OrdineRepositoryReader ordineRepositoryReader) =>
            {
                //lettura ordine origine
                Ordine ordineOrigine = await ordineRepositoryReader.GetOrdineByIdAsync(id.ToString());

                if (ordineOrigine == null)
                    return Results.NotFound();


                //creazione evento richiesta cancellazione 
                OrdineCancellazioneRichiestaEvent eventoCancellazione = new OrdineCancellazioneRichiestaEvent();
                Guid IdSaga = Guid.NewGuid();
                eventoCancellazione.IdSaga = IdSaga;
                eventoCancellazione.IdOrdine = id.ToString();

                //serializzazione messaggio per Service Bus
                string eventoRichiestoSerializzato = JsonSerializer.Serialize(eventoCancellazione);

                //caso RabbitMQ
                var bodyMessaggioRabbitMQ = Encoding.UTF8.GetBytes(eventoRichiestoSerializzato);

                //pubblicazione su exchange degli eventi di tipo TOPIC --> 
                //permette di avere più consumatori per lo stesso evento
                //si usa routing key descrittiva                
                channel.ExchangeDeclare(PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                        ExchangeType.Topic,
                                        durable: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.DURABLE);

                channel.BasicPublish(
                    exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                    routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.CANCELLAZIONE,
                    basicProperties: null,
                    body: bodyMessaggioRabbitMQ);


                return Results.Accepted(value: new RispostaAddEditOrdineDTO() { IdSaga = IdSaga.ToString() });
            })
            .WithName("RichiediCancellazioneOrdine")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "DataEntry, Admin" });

        }


    }
}
