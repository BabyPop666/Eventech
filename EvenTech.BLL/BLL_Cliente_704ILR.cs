using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EvenTech.BE;
using EvenTech.DAL;
using EvenTech.Services;

namespace EvenTech.BLL
{
    public enum ClienteResult_704ILR
    {
        Success_704ILR,
        NombreInvalido_704ILR,
        DniDuplicado_704ILR,
        EmailInvalido_704ILR,
        NotFound_704ILR
    }

    // Reglas de negocio de clientes (Proceso 1): validaciones antes de persistir.
    public static class BLL_Cliente_704ILR
    {
        private static readonly Regex EmailRegex_704ILR =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static List<BE_Cliente_704ILR> GetAll_704ILR() => DAL_Cliente_704ILR.GetAll_704ILR();

        public static BE_Cliente_704ILR GetById_704ILR(int id_704ILR) => DAL_Cliente_704ILR.GetById_704ILR(id_704ILR);

        public static ClienteResult_704ILR Crear_704ILR(BE_Cliente_704ILR c_704ILR, out int nuevoId_704ILR)
        {
            nuevoId_704ILR = 0;
            var v_704ILR = Validar_704ILR(c_704ILR, 0);
            if (v_704ILR != ClienteResult_704ILR.Success_704ILR) return v_704ILR;

            nuevoId_704ILR = DAL_Cliente_704ILR.Insert_704ILR(c_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Clientes", "Alta de cliente", CriticidadBitacora_704ILR.Info,
                $"Cliente '{c_704ILR.NombreCompleto_704ILR}' creado (#{nuevoId_704ILR})");
            return ClienteResult_704ILR.Success_704ILR;
        }

        public static ClienteResult_704ILR Actualizar_704ILR(BE_Cliente_704ILR c_704ILR)
        {
            if (c_704ILR == null || c_704ILR.Id_704ILR <= 0 || !DAL_Cliente_704ILR.Exists_704ILR(c_704ILR.Id_704ILR)) return ClienteResult_704ILR.NotFound_704ILR;
            var v_704ILR = Validar_704ILR(c_704ILR, c_704ILR.Id_704ILR);
            if (v_704ILR != ClienteResult_704ILR.Success_704ILR) return v_704ILR;

            DAL_Cliente_704ILR.Update_704ILR(c_704ILR);
            BLL_Bitacora_704ILR.Registrar_704ILR("Clientes", "Modificacion de cliente", CriticidadBitacora_704ILR.Info,
                $"Cliente #{c_704ILR.Id_704ILR} actualizado");
            return ClienteResult_704ILR.Success_704ILR;
        }

        private static ClienteResult_704ILR Validar_704ILR(BE_Cliente_704ILR c_704ILR, int idActual_704ILR)
        {
            if (c_704ILR == null || string.IsNullOrWhiteSpace(c_704ILR.Nombre_704ILR))
                return ClienteResult_704ILR.NombreInvalido_704ILR;

            // El email puede llegar todavia cifrado: pasa cuando la base se restaura en
            // otra maquina y la clave local no puede descifrar el valor guardado, asi que
            // la lectura devuelve el texto tal cual ("ENC:..."). Ese valor no es un email
            // invalido escrito por el usuario, es un dato que no se pudo abrir: rechazarlo
            // dejaria la ficha del cliente trabada sin forma de corregirla.
            if (!string.IsNullOrWhiteSpace(c_704ILR.Email_704ILR) &&
                !CryptoService_704ILR.EstaProtegido_704ILR(c_704ILR.Email_704ILR) &&
                !EmailRegex_704ILR.IsMatch(c_704ILR.Email_704ILR.Trim()))
                return ClienteResult_704ILR.EmailInvalido_704ILR;

            if (!string.IsNullOrWhiteSpace(c_704ILR.Dni_704ILR) && DAL_Cliente_704ILR.ExistsDni_704ILR(c_704ILR.Dni_704ILR.Trim(), idActual_704ILR))
                return ClienteResult_704ILR.DniDuplicado_704ILR;

            return ClienteResult_704ILR.Success_704ILR;
        }
    }
}
