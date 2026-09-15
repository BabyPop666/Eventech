namespace EvenTech.UI
{
    // Vista del area de contenido de frmMain que puede tener ediciones sin guardar.
    // frmMain la consulta antes de descartarla (al navegar a otra seccion, al volver a
    // pulsar la misma, al cerrar sesion y al cerrar la ventana) para no perder en
    // silencio lo que el usuario cargo. Cualquier vista que la implemente queda cubierta.
    internal interface IVistaConCambios_704ILR
    {
        bool HayCambiosSinGuardar_704ILR { get; }
    }
}
