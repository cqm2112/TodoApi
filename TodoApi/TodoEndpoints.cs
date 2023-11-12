using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
namespace TodoApi;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints (this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/Todo", async (TodoApiContext db, HttpContext context) =>
        {
            string Token = context.Request.Headers["UserKey"];
            var query = db.Todo.AsQueryable();

            if (!string.IsNullOrEmpty(Token))
            {
                query = query.Where(t => t.OwnId == Token);
            }

            return await query.ToListAsync();
        })
        .WithName("GetAllTodos")
        .Produces<List<Todo>>(StatusCodes.Status200OK);

        routes.MapGet("/api/Todo/{id}", async (Guid id, TodoApiContext db, HttpContext context) =>
        {
            string Token = context.Request.Headers["UserKey"];
            if (!string.IsNullOrEmpty(Token))
            {

                return await db.Todo.FindAsync(id)
                    is Todo model
                        ? Results.Ok(model)
                        : Results.NotFound();
            }
            return Results.Unauthorized();
        })
        .WithName("GetTodoById")
        .Produces<Todo>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        routes.MapPut("/api/Todo/{id}", async (Guid id, Todo todo, TodoApiContext db, HttpContext context) =>
        {
            string Token = context.Request.Headers["UserKey"];
            if (!string.IsNullOrEmpty(Token))
            {
                todo.OwnId = Token;
                var foundModel = await db.Todo.FindAsync(id);

                if (foundModel is null)
                {
                    return Results.NotFound();
                }

                db.Update(todo);

                try
                {
                    // Guardar cambios en la base de datos
                    var constresponse = await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Manejar problemas de concurrencia si es necesario
                    // Esta excepción se lanza cuando otro usuario podría haber actualizado la entidad
                    // entre el momento en que la recuperaste y el momento en que intentas guardar tus cambios
                    // Puedes implementar estrategias de resolución de conflictos aquí
                    return Results.Conflict();
                }


                return Results.NoContent();
            }
            return Results.Unauthorized();
        })
        .WithName("UpdateTodo")
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status204NoContent);

        routes.MapPost("/api/Todo/", async (Todo todo, TodoApiContext db, HttpContext context) =>
        {
            string Token = context.Request.Headers["UserKey"];
            if (!string.IsNullOrEmpty(Token)){
            todo.OwnId = Token;
            db.Todo.Add(todo);
            await db.SaveChangesAsync();
            return Results.Created($"/Todos/{todo.Id}", todo);
            }
            return Results.Unauthorized();
        })
        .WithName("CreateTodo")
        .Produces<Todo>(StatusCodes.Status201Created);

        routes.MapDelete("/api/Todo/{id}", async (Guid Id, TodoApiContext db, HttpContext context) =>
        {
            string Token = context.Request.Headers["UserKey"];
            if (!string.IsNullOrEmpty(Token))
            {
                if (await db.Todo.FindAsync(Id) is Todo todo)
                {
                    db.Todo.Remove(todo);
                    await db.SaveChangesAsync();
                    return Results.Ok(todo);
                }
            return Results.NotFound();
            }
            return Results.Unauthorized() ;

        })
        .WithName("DeleteTodo")
        .Produces<Todo>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
