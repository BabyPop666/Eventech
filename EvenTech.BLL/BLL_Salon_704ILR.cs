using System.Collections.Generic;
using EvenTech.BE;
using EvenTech.DAL;

namespace EvenTech.BLL
{
    // Acceso de negocio al catalogo de salones. La UI consume esto en lugar de
    // tocar el DAL directamente (la capa UI no referencia DAL).
    public static class BLL_Salon_704ILR
    {
        public static List<BE_Salon_704ILR> GetAll_704ILR() => DAL_Salon_704ILR.GetAll_704ILR();
    }
}
