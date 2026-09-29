## Table `personas`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_persona` | `int4` | Primary Identity |
| `tipo_documento` | `varchar` |  |
| `numero_documento` | `varchar` |  Unique |
| `nombres` | `varchar` |  |
| `apellidos` | `varchar` |  |

## Table `roles`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_rol` | `int4` | Primary Identity |
| `nombre` | `varchar` |  Unique |
| `descripcion` | `text` |  Nullable |
| `estado` | `varchar` |  |
| `creado_el` | `timestamp` |  Nullable |

## Table `usuarios`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_usuario` | `int4` | Primary Identity |
| `id_persona` | `int4` |  Unique |
| `dni_login` | `varchar` |  Unique |
| `correo` | `varchar` |  Unique |
| `clave_cifrada` | `varchar` |  Nullable |
| `estado` | `varchar` |  |
| `ultimo_acceso` | `timestamp` |  Nullable |
| `creado_el` | `timestamp` |  Nullable |
| `actualizado_el` | `timestamp` |  Nullable |

## Table `usuarios_roles`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_usuario` | `int4` | Primary |
| `id_rol` | `int4` | Primary |
| `estado` | `varchar` |  |
| `fecha_asignacion` | `timestamp` |  Nullable |

## Table `permisos_modulos`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_permiso` | `int4` | Primary Identity |
| `nombre_modulo` | `varchar` |  Unique |
| `nivel_acceso` | `varchar` |  |
| `descripcion` | `text` |  Nullable |

## Table `roles_permisos`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_rol` | `int4` | Primary |
| `id_permiso` | `int4` | Primary |

## Table `sesiones_login`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_sesion` | `uuid` | Primary |
| `id_usuario` | `int4` |  |
| `metodo_autenticacion` | `varchar` |  |
| `ip_origen` | `varchar` |  Nullable |
| `fecha_inicio` | `timestamp` |  |
| `fecha_expiracion` | `timestamp` |  |
| `estado` | `varchar` |  |

## Table `especialidad`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_especialidad` | `int4` | Primary Identity |
| `nombre` | `varchar` |  |
| `estado` | `varchar` |  |

## Table `almacen`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_almacen` | `int4` | Primary Identity |
| `id_especialidad` | `int4` |  |
| `nombre` | `varchar` |  |
| `ubicacion` | `varchar` |  Nullable |
| `responsable` | `varchar` |  Nullable |
| `estado` | `varchar` |  |

## Table `ambiente`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_ambiente` | `int4` | Primary Identity |
| `id_especialidad` | `int4` |  |
| `nombre` | `varchar` |  |
| `estado` | `varchar` |  |

## Table `bien`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_bien` | `int4` | Primary Identity |
| `id_almacen` | `int4` |  |
| `cbi` | `varchar` |  Nullable Unique |
| `descripcion` | `varchar` |  |
| `tipo_bien` | `varchar` |  |
| `marca` | `varchar` |  Nullable |
| `modelo` | `varchar` |  Nullable |
| `serie` | `varchar` |  Nullable Unique |
| `procedencia` | `varchar` |  Nullable |
| `fecha_ingreso` | `date` |  |
| `estado` | `varchar` |  |
| `observacion` | `text` |  Nullable |

## Table `asignacion`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_asignacion` | `int4` | Primary Identity |
| `id_bien` | `int4` |  |
| `id_ambiente` | `int4` |  |
| `responsable` | `varchar` |  |
| `fecha_asignacion` | `date` |  |
| `fecha_fin` | `date` |  Nullable |
| `estado` | `varchar` |  |
| `observacion` | `text` |  Nullable |

## Table `transferencia`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_transferencia` | `int4` | Primary Identity |
| `id_bien` | `int4` |  |
| `id_ambiente_origen` | `int4` |  |
| `id_ambiente_destino` | `int4` |  |
| `fecha` | `date` |  |
| `motivo` | `varchar` |  |
| `dni_responsable` | `bpchar` |  |
| `observacion` | `text` |  Nullable |

## Table `inventario_anual`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_inventario` | `int4` | Primary Identity |
| `id_bien` | `int4` |  |
| `anio` | `int2` |  |
| `fecha_verificacion` | `date` |  |
| `resultado` | `varchar` |  |
| `observacion` | `text` |  Nullable |

## Table `incidencias`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_incidencia` | `int4` | Primary Identity |
| `id_bien` | `int4` |  |
| `reportador_id` | `int4` |  |
| `tecnico_id` | `int4` |  Nullable |
| `fecha_reporte` | `timestamp` |  |
| `fecha_cierre` | `timestamp` |  Nullable |
| `tipo_falla` | `varchar` |  |
| `gravedad` | `varchar` |  Nullable |
| `descripcion` | `text` |  |
| `solucion_aplicada` | `text` |  Nullable |
| `estado` | `varchar` |  Nullable |

## Table `requerimientos`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_requerimiento` | `int4` | Primary Identity |
| `solicitante_id` | `int4` |  |
| `tecnico_id` | `int4` |  Nullable |
| `id_ambiente` | `int4` |  Nullable |
| `tipo_servicio` | `varchar` |  |
| `prioridad` | `varchar` |  |
| `descripcion` | `text` |  |
| `fecha_solicitud` | `timestamp` |  |
| `fecha_atencion` | `timestamp` |  Nullable |
| `estado` | `varchar` |  |
| `observacion_resolucion` | `text` |  Nullable |

## Table `botica_medicamentos`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_medicamento` | `int4` | Primary Identity |
| `nombre` | `varchar` |  |
| `presentacion` | `varchar` |  |
| `stock_actual` | `int4` |  |
| `fecha_vencimiento` | `date` |  |
| `estado` | `varchar` |  Nullable |

## Table `atenciones_topico`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_atencion_top` | `int4` | Primary Identity |
| `persona_id` | `int4` |  |
| `atendido_por_id` | `int4` |  |
| `fecha_hora` | `timestamp` |  |
| `motivo` | `varchar` |  |
| `medicamento_id` | `int4` |  Nullable |
| `es_alergico` | `bool` |  |
| `detalle_alergia` | `varchar` |  Nullable |
| `seguro_salud` | `varchar` |  Nullable |
| `observacion` | `text` |  Nullable |

## Table `atenciones_psicologia`

### Columns

| Name | Type | Constraints |
|------|------|-------------|
| `id_atencion_psico` | `int4` | Primary Identity |
| `persona_id` | `int4` |  |
| `psicologo_id` | `int4` |  |
| `fecha_hora` | `timestamp` |  |
| `motivo_consulta` | `varchar` |  |
| `observacion_notas` | `text` |  Nullable |
| `estado_atencion` | `varchar` |  Nullable |

