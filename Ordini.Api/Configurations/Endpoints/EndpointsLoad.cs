using Microsoft.AspNetCore.Authorization;
using Ordini.Api.Domains.Repositories.Dapper;
using Ordini.Api.Helpers.Mapper;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Ritorno;
using Ordini.Contracts.Models;

namespace Ordini.Api.Configurations.Endpoints
{
    public static class EndpointsLoad
    {

        //caricamento endpoints
        public static void MapEndpoints(IEndpointRouteBuilder app)
        {
            //endpoint root
            app.MapGet("/", () => "Ordini Web API avviata!");

            var ordiniGroup = app.MapGroup("/api/ordini").RequireAuthorization();
            ordiniGroup.MapGet("/{id}", async (string id, OrdineRepositoryReader ordineRepositoryReader) =>
            {
                Ordine ordineMD = await ordineRepositoryReader.GetOrdineByIdAsync(id);

                if (ordineMD == null)
                    return Results.NotFound();

                OrdineDTO ritorno = OrdineModelToDTO.MapOrdineToDTO(ordineMD);
                return Results.Ok(ritorno);

            }).WithName("GetOrdineByID")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "User,Admin" });


        }
    }
}
