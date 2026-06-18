using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    public enum ClienteResult
    {
        Success,
        NombreInvalido,
        DniDuplicado,
        EmailInvalido,
        NotFound
    }

    // Reglas de negocio de clientes (Proceso 1): validaciones antes de persistir.
    public static class BLL_Cliente
    {
        private static readonly Regex EmailRegex =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static List<BE_Cliente> GetAll() => DAL_Cliente.GetAll();

        public static BE_Cliente GetById(int id) => DAL_Cliente.GetById(id);

        public static ClienteResult Crear(BE_Cliente c, out int nuevoId)
        {
            nuevoId = 0;
            var v = Validar(c, 0);
            if (v != ClienteResult.Success) return v;

            nuevoId = DAL_Cliente.Insert(c);
            BLL_Bitacora.Registrar("Clientes", "Alta de cliente", CriticidadBitacora.Info,
                $"Cliente '{c.NombreCompleto}' creado (#{nuevoId})");
            return ClienteResult.Success;
        }

        public static ClienteResult Actualizar(BE_Cliente c)
        {
            if (c == null || c.Id <= 0 || !DAL_Cliente.Exists(c.Id)) return ClienteResult.NotFound;
            var v = Validar(c, c.Id);
            if (v != ClienteResult.Success) return v;

            DAL_Cliente.Update(c);
            BLL_Bitacora.Registrar("Clientes", "Modificacion de cliente", CriticidadBitacora.Info,
                $"Cliente #{c.Id} actualizado");
            return ClienteResult.Success;
        }

        private static ClienteResult Validar(BE_Cliente c, int idActual)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.Nombre))
                return ClienteResult.NombreInvalido;

            if (!string.IsNullOrWhiteSpace(c.Email) && !EmailRegex.IsMatch(c.Email.Trim()))
                return ClienteResult.EmailInvalido;

            if (!string.IsNullOrWhiteSpace(c.Dni) && DAL_Cliente.ExistsDni(c.Dni.Trim(), idActual))
                return ClienteResult.DniDuplicado;

            return ClienteResult.Success;
        }
    }
}
