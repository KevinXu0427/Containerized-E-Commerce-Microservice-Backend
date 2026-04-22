using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGateway.Swagger;

public sealed class OcelotRoutesDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths ??= new OpenApiPaths();

        static OpenApiResponses DefaultJsonResponses(bool include404 = false)
        {
            var r = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "Success",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema { Type = JsonSchemaType.Object }
                        }
                    }
                }
            };
            if (include404)
            {
                r["404"] = new OpenApiResponse { Description = "Not found" };
            }

            return r;
        }

        OpenApiOperation Op(string summary, string? description = null, bool notFound = false) => new()
        {
            Summary = summary,
            Description = description,
            Tags = new HashSet<OpenApiTagReference>
            {
                new OpenApiTagReference("Gateway (Ocelot)", swaggerDoc)
            },
            Responses = DefaultJsonResponses(notFound)
        };

        static JsonNode? ExampleNode(string json)
        {
            try
            {
                return JsonNode.Parse(json);
            }
            catch (JsonException)
            {
                return JsonValue.Create(json);
            }
        }

        static OpenApiRequestBody JsonBody(string exampleJson) => new()
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema { Type = JsonSchemaType.Object },
                    Example = ExampleNode(exampleJson)
                }
            }
        };

        void Add(string path, HttpMethod method, OpenApiOperation operation)
        {
            if (!swaggerDoc.Paths.TryGetValue(path, out var item) || item is not OpenApiPathItem pathItem)
            {
                pathItem = new OpenApiPathItem();
                swaggerDoc.Paths[path] = pathItem;
            }

            pathItem.AddOperation(method, operation);
        }

        static OpenApiParameter PathInt(string name) => new()
        {
            Name = name,
            In = ParameterLocation.Path,
            Required = true,
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
        };

        var orderId = PathInt("id");
        var custId = PathInt("id");
        var prodId = PathInt("id");
        var invPid = PathInt("productId");

        // Orders
        Add("/gateway/orders", HttpMethod.Get, Op("List orders", "Forwards to OrderService `GET /api/orders`"));

        var postOrder = Op("Create order", "Forwards to OrderService `POST /api/orders`");
        postOrder.RequestBody = JsonBody("""{"customerId":1,"productId":1,"quantity":2,"discountPercent":0}""");
        Add("/gateway/orders", HttpMethod.Post, postOrder);

        var getOneOrder = Op("Get order by id", "Forwards to OrderService `GET /api/orders/{id}`", notFound: true);
        getOneOrder.Parameters = [orderId];
        Add("/gateway/orders/{id}", HttpMethod.Get, getOneOrder);

        var putStatus = Op("Update order status", "Forwards to `PUT /api/orders/{id}/status`; use status=Cancelled to cancel");
        putStatus.Parameters = [orderId];
        putStatus.RequestBody = JsonBody("""{"status":"Cancelled"}""");
        Add("/gateway/orders/{id}/status", HttpMethod.Put, putStatus);

        // Customers
        Add("/gateway/customers", HttpMethod.Get, Op("List customers", "Forwards to CustomerService `GET /api/customers`"));

        var postCust = Op("Create customer", "Forwards to CustomerService `POST /api/customers`");
        postCust.RequestBody = JsonBody("""{"name":"Alice","email":"a@example.com"}""");
        Add("/gateway/customers", HttpMethod.Post, postCust);

        var getCust = Op("Get customer by id", "Forwards to CustomerService `GET /api/customers/{id}`", notFound: true);
        getCust.Parameters = [custId];
        Add("/gateway/customers/{id}", HttpMethod.Get, getCust);

        // Products
        Add("/gateway/products", HttpMethod.Get, Op("List products", "Forwards to ProductService `GET /api/products`"));

        var postProd = Op("Create product", "Forwards to ProductService `POST /api/products`");
        postProd.RequestBody = JsonBody("""{"name":"Book","price":19.99}""");
        Add("/gateway/products", HttpMethod.Post, postProd);

        var getProd = Op("Get product by id", "Forwards to ProductService `GET /api/products/{id}`", notFound: true);
        getProd.Parameters = [prodId];
        Add("/gateway/products/{id}", HttpMethod.Get, getProd);

        var putProd = Op("Update product", "Forwards to ProductService `PUT /api/products/{id}`");
        putProd.Parameters = [prodId];
        putProd.RequestBody = JsonBody("""{"name":"Book","price":9.99}""");
        Add("/gateway/products/{id}", HttpMethod.Put, putProd);

        var delProd = Op("Delete product", "Forwards to ProductService `DELETE /api/products/{id}`");
        delProd.Parameters = [prodId];
        Add("/gateway/products/{id}", HttpMethod.Delete, delProd);

        // Inventory
        Add("/gateway/inventory", HttpMethod.Get, Op("List inventory rows", "Forwards to InventoryService `GET /api/inventory`"));

        var getInv = Op("Get inventory by product id", "Forwards to InventoryService `GET /api/inventory/{productId}`", notFound: true);
        getInv.Parameters = [invPid];
        Add("/gateway/inventory/{productId}", HttpMethod.Get, getInv);

        var postCreateInv = Op("Create or replace stock", "Forwards to `POST /api/inventory/createOrUpdate`");
        postCreateInv.RequestBody = JsonBody("""{"productId":1,"quantity":100}""");
        Add("/gateway/inventory/createOrUpdate", HttpMethod.Post, postCreateInv);

        var postRes = Op("Reserve stock", "Forwards to `POST /api/inventory/reserve`");
        postRes.RequestBody = JsonBody("""{"productId":1,"quantity":1}""");
        Add("/gateway/inventory/reserve", HttpMethod.Post, postRes);

        var postRel = Op("Release stock", "Forwards to `POST /api/inventory/release`");
        postRel.RequestBody = JsonBody("""{"productId":1,"quantity":1}""");
        Add("/gateway/inventory/release", HttpMethod.Post, postRel);
    }
}
