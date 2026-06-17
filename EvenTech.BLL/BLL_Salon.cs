using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    // Acceso de negocio al catalogo de salones. La UI consume esto en lugar de
    // tocar el DAL directamente (la capa UI no referencia DAL).
    public static class BLL_Salon
    {
        public static List<BE_Salon> GetAll() => DAL_Salon.GetAll();
    }
}
