using Microsoft.AspNetCore.Authorization;
using Ordini.Api.Domains.Repositories.Dapper;
using Ordini.Api.Helpers.Mapper;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Ritorno;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Models;

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
            //ordiniGroup.MapPost("/", (AddOrdineDTO nuovoOrdine, IModel channel) =>
            //{
            //    Ordine o = OrdineDTOToModel.MapOrdine(nuovoOrdine);
            //    o.Id = Guid.NewGuid().ToString();

            //creazione evento
            OrdineCreatoEvent eventoCreato = new OrdineCreatoEvent();
            //eventoCreato.
            //OrdineRichiestoEvent evento = new OrdineRichiestoEvent
            //{
            //    IdSaga = o.Id,
            //    Ordine
            //}
            ////serializzazione messaggio per Service Bus
            //string ordineSerializzato = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<AddOrdineDTO>(nuovoOrdine);

            ////caso RabbitMQ
            //var properties =

            //}

        }
    }
}
