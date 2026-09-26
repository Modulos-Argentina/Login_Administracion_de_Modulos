-- ====================================================================================
-- MÓDULO 8: AUTENTICACIÓN, ROLES Y SEGURIDAD (PostgreSQL)
-- Lógica: Login Dual (DNI local o Google SSO por validación de correo único)
-- ====================================================================================

-- (Opcional/Referencia) Creación mínima de la tabla 'personas' del Módulo 1 
-- para que las llaves foráneas de este script funcionen si lo pruebas desde cero.
CREATE TABLE IF NOT EXISTS personas (
    id_persona INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tipo_documento VARCHAR(10) NOT NULL,
    numero_documento VARCHAR(20) UNIQUE NOT NULL,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL
);

-- ------------------------------------------------------------------------------------
-- 1. TABLA: roles
-- Catálogo de perfiles del sistema (Alumno, Docente, Psicología, Enfermería, etc.)
-- ------------------------------------------------------------------------------------
CREATE TABLE roles (
    id_rol INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre VARCHAR(50) UNIQUE NOT NULL,
    descripcion TEXT,
    estado VARCHAR(15) DEFAULT 'Activo' NOT NULL,
    creado_el TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Regla de Dominio: Solo permite estados válidos
    CONSTRAINT chk_roles_estado CHECK (estado IN ('Activo', 'Inactivo'))
);

-- ------------------------------------------------------------------------------------
-- 2. TABLA: usuarios
-- Núcleo de acceso. Soporta login local (DNI + Clave) o Google SSO (Correo + Sin Clave)
-- ------------------------------------------------------------------------------------
CREATE TABLE usuarios (
    id_usuario INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_persona INTEGER NOT NULL,
    dni_login VARCHAR(20) UNIQUE NOT NULL,
    correo VARCHAR(100) UNIQUE NOT NULL,
    clave_cifrada VARCHAR(255), -- NULLABLE: Si solo entra con Google, no necesita clave
    estado VARCHAR(15) DEFAULT 'Activo' NOT NULL,
    ultimo_acceso TIMESTAMP,
    creado_el TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    actualizado_el TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    -- Relación con Módulo 1 (Cada usuario pertenece a una única persona)
    CONSTRAINT fk_usuarios_persona FOREIGN KEY (id_persona) REFERENCES personas(id_persona) ON DELETE RESTRICT,
    -- Regla: Una persona solo puede tener una cuenta de usuario en todo el instituto
    CONSTRAINT uq_usuarios_persona UNIQUE (id_persona),
    -- Regla de Dominio: Control estricto del estado de la cuenta
    CONSTRAINT chk_usuarios_estado CHECK (estado IN ('Activo', 'Inactivo', 'Bloqueado'))
);

-- ------------------------------------------------------------------------------------
-- 3. TABLA: usuarios_roles
-- Rompe la relación muchos a muchos. Un usuario (Docente) puede ser también (Soporte).
-- ------------------------------------------------------------------------------------
CREATE TABLE usuarios_roles (
    id_usuario INTEGER NOT NULL,
    id_rol INTEGER NOT NULL,
    estado VARCHAR(15) DEFAULT 'Activo' NOT NULL,
    fecha_asignacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id_usuario, id_rol), -- Regla: Un usuario no puede tener el mismo rol 2 veces
    CONSTRAINT fk_ur_usuario FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario) ON DELETE CASCADE,
    CONSTRAINT fk_ur_rol FOREIGN KEY (id_rol) REFERENCES roles(id_rol) ON DELETE RESTRICT,
    CONSTRAINT chk_ur_estado CHECK (estado IN ('Activo', 'Revocado'))
);

-- ------------------------------------------------------------------------------------
-- 4. TABLA: permisos_modulos (RBAC)
-- Define a qué partes del sistema da acceso un rol (ej. 'Topico_Enfermeria')
-- ------------------------------------------------------------------------------------
CREATE TABLE permisos_modulos (
    id_permiso INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre_modulo VARCHAR(100) UNIQUE NOT NULL,
    nivel_acceso VARCHAR(20) NOT NULL,
    descripcion TEXT
);

-- ------------------------------------------------------------------------------------
-- 5. TABLA: roles_permisos
-- Asigna los permisos específicos a cada rol.
-- ------------------------------------------------------------------------------------
CREATE TABLE roles_permisos (
    id_rol INTEGER NOT NULL,
    id_permiso INTEGER NOT NULL,

    PRIMARY KEY (id_rol, id_permiso),
    CONSTRAINT fk_rp_rol FOREIGN KEY (id_rol) REFERENCES roles(id_rol) ON DELETE CASCADE,
    CONSTRAINT fk_rp_permiso FOREIGN KEY (id_permiso) REFERENCES permisos_modulos(id_permiso) ON DELETE CASCADE
);

-- ------------------------------------------------------------------------------------
-- 6. TABLA: sesiones_login
-- Auditoría de seguridad: Registra CÓMO entró el usuario (DNI o Google) y desde dónde.
-- ------------------------------------------------------------------------------------
CREATE TABLE sesiones_login (
    id_sesion UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    id_usuario INTEGER NOT NULL,
    metodo_autenticacion VARCHAR(20) NOT NULL,
    ip_origen VARCHAR(45),
    fecha_inicio TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
    fecha_expiracion TIMESTAMP NOT NULL,
    estado VARCHAR(15) DEFAULT 'Activa' NOT NULL,

    CONSTRAINT fk_sesiones_usuario FOREIGN KEY (id_usuario) REFERENCES usuarios(id_usuario) ON DELETE CASCADE,
    CONSTRAINT chk_sesiones_metodo CHECK (metodo_autenticacion IN ('DNI_LOCAL', 'GOOGLE_SSO')),
    CONSTRAINT chk_sesiones_estado CHECK (estado IN ('Activa', 'Expirada', 'Cerrada'))
);

-- ====================================================================================
-- INSERCIÓN DE DATOS POR DEFECTO (CATÁLOGOS)
-- ====================================================================================

-- Insertar los roles solicitados
INSERT INTO roles (nombre, descripcion) VALUES
('Alumno', 'Estudiante matriculado o egresado del instituto'),
('Docente', 'Personal docente con carga académica'),
('Enfermería (Tópico)', 'Personal de salud a cargo de las atenciones físicas (Módulo 5)'),
('Psicología', 'Personal a cargo del bienestar mental y citas psicológicas (Módulo 5)'),
('Jefe de Soporte', 'Jefatura de laboratorios, inventario y practicantes'),
('Practicante', 'Estudiante en horas de práctica de soporte (Módulo 6)'),
('Secretaría Académica', 'Motor operativo, trámites y egresos'),
('Administrador', 'Acceso total y configuración del sistema');

-- Insertar algunos permisos clave como ejemplo
INSERT INTO permisos_modulos (nombre_modulo, nivel_acceso, descripcion) VALUES
('portal_academico', 'Total', 'Acceso a notas, sílabos y asistencias'),
('topico_salud', 'Total', 'Acceso a historiales médicos de alumnos (Privado)'),
('bienestar_psicologico', 'Total', 'Acceso a expedientes psicológicos (Privado)'),
('soporte_inventario', 'Total', 'Gestión de reservas y mantenimiento de equipos');

-- Enlazar los roles sensibles con sus módulos (Ejemplo)
-- Asignar permiso "topico_salud" (id 2) a "Enfermería (Tópico)" (id 3)
INSERT INTO roles_permisos (id_rol, id_permiso) VALUES (3, 2);
-- Asignar permiso "bienestar_psicologico" (id 3) a "Psicología" (id 4)
INSERT INTO roles_permisos (id_rol, id_permiso) VALUES (4, 3);
-- Asignar permiso "soporte_inventario" (id 4) a "Jefe de Soporte" (id 5)
INSERT INTO roles_permisos (id_rol, id_permiso) VALUES (5, 4);

-- ====================================================================================
-- FUNCIÓN / TRIGGER PARA TRAZABILIDAD AUTOMÁTICA
-- (Asegura que el campo actualizado_el cambie solo cada vez que modifican un usuario)
-- ====================================================================================
CREATE OR REPLACE FUNCTION actualizar_timestamp()
RETURNS TRIGGER AS $$
BEGIN
   NEW.actualizado_el = CURRENT_TIMESTAMP;
   RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER tr_usuarios_actualizado
BEFORE UPDATE ON usuarios
FOR EACH ROW
EXECUTE FUNCTION actualizar_timestamp();

-- ====================================================================================
-- ACTUALIZACIÓN: ARQUITECTURA MVC BÁSICA PARA EL LOGIN
-- ====================================================================================
/*
Se implementó una arquitectura MVC sencilla para el login:
1. **Modelo**: Se creó la clase `Model/Usuario.cs` para mapear los datos del usuario extraídos de la tabla `usuarios`.
2. **Controlador**: Se creó `Controlador/LoginController.cs` con métodos `Index` (GET y POST) para procesar las credenciales contra la base de datos PostgreSQL utilizando ADO.NET (`NpgsqlConnection`).
3. **Vista**: Se modificó `Vista/Index.cshtml` para eliminar las directivas de Razor Pages (`@page`, `@model`) y conectarlo al `LoginController`.
4. **Configuración**: Se actualizó `Program.cs` para usar `AddControllersWithViews()` y `MapControllerRoute()`, y se corrigió el archivo `appsettings.json` garantizando un formato JSON válido para la cadena de conexión.
Si el usuario se autentica correctamente y está activo, es redirigido a una página en blanco (`/Login/Bienvenido`). De lo contrario, se muestra un mensaje de error en la misma vista de login.
*/