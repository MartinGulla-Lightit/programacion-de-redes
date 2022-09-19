using System;
namespace Protocolo;

public static class Constantes
{
    public const int LargoFijo = 4;
    public const int Header = 3;
    public const int Command = 1;

    public const int Login = 1;
    public const int Registrarse = 2;
    public const int Logout = 3;

    public const int RespuestaLoginExistoso = 1;
    public const int RespuestaLoginFallido = 0;
    public const int RespuestaRegistrarseExistoso = 2;
    public const int RespuestaRegistrarseFallido = 0;

    public const int AltaPerfilTrabajo = 4;
    public const int RespuestaAltaPerfilTrabajoExistoso = 4;
    public const int RespuestaAltaPerfilTrabajoFallido = 0;
    public const int ListarPerfilesTrabajo = 5;
    public const int RespuestaListarPerfilesTrabajoExitoso = 5;
    public const int RespuestaListarPerfilesTrabajoFallido = 0;
    public const int ConsultarPerfilEspecifico = 6;
    public const int RespuestaConsultarPerfilEspecificoExitoso = 6;
    public const int RespuestaConsultarPerfilEspecificoFallido = 0;
    
}
