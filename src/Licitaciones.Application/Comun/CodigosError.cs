namespace Licitaciones.Application.Comun;

/// <summary>
/// Códigos de error del dominio. Son parte del contrato público de la API: los clientes
/// pueden ramificar sobre ellos, así que no cambian aunque cambie la redacción del
/// mensaje.
/// </summary>
public static class CodigosError
{
    /// <summary>Ya existe un proveedor con ese nombre normalizado.</summary>
    public const string ProveedorDuplicado = "PROVEEDOR_DUPLICADO";

    /// <summary>El nombre del proveedor usa caracteres no admitidos.</summary>
    public const string NombreProveedorInvalido = "NOMBRE_PROVEEDOR_INVALIDO";

    /// <summary>El proveedor solicitado no existe.</summary>
    public const string ProveedorNoEncontrado = "PROVEEDOR_NO_ENCONTRADO";

    /// <summary>Ya existe una licitación con ese código normalizado.</summary>
    public const string CodigoLicitacionDuplicado = "CODIGO_LICITACION_DUPLICADO";

    /// <summary>La licitación solicitada no existe.</summary>
    public const string LicitacionNoEncontrada = "LICITACION_NO_ENCONTRADA";

    /// <summary>El monto no cumple las reglas monetarias del sistema.</summary>
    public const string MontoInvalido = "MONTO_INVALIDO";

    /// <summary>La transición de estado solicitada no está permitida.</summary>
    public const string TransicionInvalida = "TRANSICION_INVALIDA";

    /// <summary>No puede publicarse una licitación cuya fecha de cierre ya pasó.</summary>
    public const string FechaCierreEnElPasado = "FECHA_CIERRE_EN_EL_PASADO";

    /// <summary>El presupuesto quedaría por debajo de una oferta ya registrada.</summary>
    public const string PresupuestoMenorQueOferta = "PRESUPUESTO_MENOR_QUE_OFERTA";

    /// <summary>El proveedor ya registró una oferta para esa licitación.</summary>
    public const string OfertaDuplicada = "OFERTA_DUPLICADA";

    /// <summary>La oferta supera el presupuesto de la licitación.</summary>
    public const string OfertaSuperaPresupuesto = "OFERTA_SUPERA_PRESUPUESTO";

    /// <summary>La licitación todavía no está publicada.</summary>
    public const string LicitacionNoPublicada = "LICITACION_NO_PUBLICADA";

    /// <summary>La licitación ya cerró y no admite más ofertas.</summary>
    public const string LicitacionCerrada = "LICITACION_CERRADA";

    /// <summary>La oferta solicitada no existe.</summary>
    public const string OfertaNoEncontrada = "OFERTA_NO_ENCONTRADA";

    /// <summary>La oferta pertenece a una licitación cerrada y no admite cambios.</summary>
    public const string OfertaInmutable = "OFERTA_INMUTABLE";

    /// <summary>El rango de aprobación se traslapa con otro ya existente.</summary>
    public const string RangoTraslapado = "RANGO_TRASLAPADO";

    /// <summary>Ya existe un nivel de aprobación sin monto máximo.</summary>
    public const string RangoAbiertoDuplicado = "RANGO_ABIERTO_DUPLICADO";

    /// <summary>El rango de aprobación no es coherente.</summary>
    public const string RangoInvalido = "RANGO_INVALIDO";

    /// <summary>El nivel de aprobación solicitado no existe.</summary>
    public const string NivelAprobacionNoEncontrado = "NIVEL_APROBACION_NO_ENCONTRADO";

    /// <summary>Ningún nivel de aprobación cubre el monto consultado.</summary>
    public const string SinNivelAplicable = "SIN_NIVEL_APLICABLE";

    /// <summary>La tasa de cambio no cumple las reglas del sistema.</summary>
    public const string TasaInvalida = "TASA_INVALIDA";

    /// <summary>No hay ninguna tasa activa con la que convertir.</summary>
    public const string SinTipoCambioActivo = "SIN_TIPO_CAMBIO_ACTIVO";

    /// <summary>El tipo de cambio solicitado no existe.</summary>
    public const string TipoCambioNoEncontrado = "TIPO_CAMBIO_NO_ENCONTRADO";
}
