using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompraController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CompraController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetAllCompras")]
        public string GetCompras()
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Compra;", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<Compra> compraList = new List<Compra>();
            Response response = new Response();

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Compra compra = new Compra();
                    compra.idCompra = Convert.ToInt32(dt.Rows[i]["idCompra"]);
                    compra.idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]);
                    compra.fechaCompra = Convert.ToDateTime(dt.Rows[i]["fechaCompra"]);
                    compra.TotalCompra = Convert.ToDecimal(dt.Rows[i]["TotalCompra"]);
                    compra.Estado = Convert.ToString(dt.Rows[i]["Estado"]);

                    compra.FechaPago = dt.Rows[i]["FechaPago"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[i]["FechaPago"]) : (DateTime?)null;
                    compra.MetodoPago = Convert.ToString(dt.Rows[i]["MetodoPago"]);
                    compraList.Add(compra);
                }
            }

            if (compraList.Count > 0)
                return JsonConvert.SerializeObject(compraList);
            else
            {
                response.StatusCode = 100;
                response.ErrorMessage = "No data found";
                return JsonConvert.SerializeObject(response);
            }
        }
        [HttpPost]
        [Route("AgregarCompra")]
        public IActionResult AgregarCompra([FromBody] CompraConDetalles compraConDetalles)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                // Iniciar transacción
                con.Open();
                using SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    // 1. Insertar la compra principal
                    string queryCompra = @"INSERT INTO Compra (idProveedor, fechaCompra, TotalCompra, Estado, FechaPago, MetodoPago)
                                 OUTPUT INSERTED.idCompra
                                 VALUES (@idProveedor, @fechaCompra, @TotalCompra, @Estado, @FechaPago, @MetodoPago)";

                    int idCompra;

                    using (SqlCommand cmd = new SqlCommand(queryCompra, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@idProveedor", compraConDetalles.Compra.idProveedor);
                        cmd.Parameters.AddWithValue("@fechaCompra", compraConDetalles.Compra.fechaCompra);
                        cmd.Parameters.AddWithValue("@TotalCompra", compraConDetalles.Compra.TotalCompra);
                        cmd.Parameters.AddWithValue("@Estado", compraConDetalles.Compra.Estado ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FechaPago", compraConDetalles.Compra.FechaPago ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MetodoPago", compraConDetalles.Compra.MetodoPago ?? (object)DBNull.Value);

                        idCompra = (int)cmd.ExecuteScalar();
                    }

                    // 2. Insertar los detalles de la compra
                    if (compraConDetalles.Detalles != null && compraConDetalles.Detalles.Any())
                    {
                        string queryDetalle = @"INSERT INTO DetalleCompra 
                                    (IdCompra, CodigoProducto, Cantidad, PrecioUnitario, Subtotal)
                                    VALUES 
                                    (@IdCompra, @CodigoProducto, @Cantidad, @PrecioUnitario, @Subtotal)";

                        foreach (var detalle in compraConDetalles.Detalles)
                        {
                            using (SqlCommand cmdDetalle = new SqlCommand(queryDetalle, con, transaction))
                            {
                                decimal subtotal = detalle.Cantidad * detalle.PrecioUnitario;

                                cmdDetalle.Parameters.AddWithValue("@IdCompra", idCompra);
                                cmdDetalle.Parameters.AddWithValue("@CodigoProducto", detalle.CodigoProducto);
                                cmdDetalle.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
                                cmdDetalle.Parameters.AddWithValue("@Subtotal", subtotal);

                                cmdDetalle.ExecuteNonQuery();
                            }
                        }
                    }

                    // Commit si todo sale bien
                    transaction.Commit();
                    return Ok(new CompraResponse
                    {
                        idCompra = idCompra,
                        message = "Compra registrada exitosamente"
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new
                    {
                        error = $"Error interno del servidor: {ex.Message}"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error de conexión: {ex.Message}");
            }
        }
        // Clases DTO para la solicitud
        public class CompraConDetalles
        {
            public Compra Compra { get; set; }
            public List<DetalleCompraDto> Detalles { get; set; }
        }

        public class DetalleCompraDto
        {
            public int CodigoProducto { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        [HttpDelete]
        [Route("EliminarCompra/{id}")]
        public IActionResult EliminarCompra(int id)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = "DELETE FROM Compra WHERE idCompra = @id";
            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@id", id);
            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            return Ok(rowsAffected > 0);
        }

        [HttpPut]
        [Route("ActualizarCompra/{id}")]
        public IActionResult ActualizarCompra(int id, [FromBody] Compra compra)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"UPDATE Compra SET 
                                idProveedor = @idProveedor,
                                fechaCompra = @fechaCompra,
                                TotalCompra = @TotalCompra,
                                Estado = @Estado,
                                CreadoPor = @CreadoPor,
                                FechaPago = @FechaPago,
                                MetodoPago = @MetodoPago
                             WHERE idCompra = @idCompra";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@idCompra", id);
            cmd.Parameters.AddWithValue("@idProveedor", compra.idProveedor);
            cmd.Parameters.AddWithValue("@fechaCompra", compra.fechaCompra);
            cmd.Parameters.AddWithValue("@TotalCompra", compra.TotalCompra);
            cmd.Parameters.AddWithValue("@Estado", compra.Estado ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@FechaPago", compra.FechaPago ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@MetodoPago", compra.MetodoPago ?? (object)DBNull.Value);

            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
                return Ok(new { message = "Compra actualizada correctamente." });
            else
                return NotFound(new { message = "Compra no encontrada." });
        }
    }
}