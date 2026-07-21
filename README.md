# Block Machine

Aplicación Windows para bloquear la computadora en horarios nocturnos y mostrar mensajes personalizados de descanso.

Pensada para familiares con demencia senil que se levantan de madrugada a usar la PC.

## Requisitos

- Windows 10/11 (probado en Windows 11 Home)
- [.NET 10 Runtime](https://dotnet.microsoft.com/download) (o compilar como ejecutable autocontenido)

## Instalación rápida

### Opción A: Ejecutar desde código fuente

```bash
cd "Block Machine v2"
dotnet run --project src/BlockMachine
```

### Opción B: Instalador automático (recomendado)

Desde la carpeta del proyecto, **doble clic** en `instalar.bat` o ejecuta en terminal:

```bat
instalar.bat --compilar
```

El script:
1. Compila el `.exe` (con `--compilar`) o usa uno ya generado
2. Crea `C:\BlockMachine\`
3. Copia `BlockMachine.exe` ahí
4. Abre el asistente inicial para que configures contraseña e inicio con Windows

> No requiere permisos de administrador. No usa `Program Files`.

### Opción C: Publicar manualmente un .exe

```bash
dotnet publish src/BlockMachine -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

El ejecutable quedará en:

`src/BlockMachine/bin/Release/net10.0-windows/win-x64/publish/BlockMachine.exe`

Luego ejecuta `instalar.bat` (sin `--compilar` si el exe ya existe) o copia el `.exe` a `C:\BlockMachine\` y ábrelo una vez.

**Pasos en el asistente inicial:**

1. Crea la **contraseña de administrador**
2. Marca **Iniciar automáticamente con Windows**
3. Marca **Crear acceso directo en el escritorio**
4. A partir de ahí, la app corre sola en segundo plano

## ¿Se instala como daemon al prender Windows?

**Sí, si activas "Iniciar automáticamente con Windows"** (viene marcado por defecto).

Cómo funciona:

- Block Machine **no es un servicio de Windows** (no necesita permisos especiales)
- Se registra en el inicio de sesión del usuario (`Registro de Windows → Inicio`)
- Al encender la PC e iniciar sesión, **arranca solo** en la bandeja del sistema (icono de escudo)
- Queda vigilando el horario y bloquea cuando corresponde, sin que tu papá tenga que abrir nada

> Importante: el instalador usa `C:\BlockMachine\` por defecto. No muevas el `.exe` después de instalar; si lo haces, vuelve a ejecutar `instalar.bat` o abre la app y guarda la configuración para actualizar el inicio automático.

## Panel de administración — ¿cómo acceder?

Hay **3 formas**, todas piden **contraseña de administrador**:

| Método | Cómo |
|--------|------|
| **Acceso directo en escritorio** | Doble clic en `Block Machine - Administración` → contraseña → panel |
| **Bandeja del sistema** | Clic derecho en el icono de escudo (junto al reloj) → **Panel de administración** |
| **Línea de comandos** | `BlockMachine.exe --admin` |

Tu papá **no necesita** usar ninguna de estas rutas. Solo tú, con la contraseña.

## Acceso directo en el escritorio

Durante la instalación inicial (o desde Configuración) puedes marcar:

**"Crear acceso directo en el escritorio: Block Machine - Administración"**

Eso crea un icono visible en el escritorio que abre el panel admin. Siempre pide contraseña antes de mostrar la configuración.

- Si la app **ya está corriendo**, el acceso directo abre el panel en la instancia activa
- Si **no está corriendo**, la inicia y abre el panel

## Primer uso

1. Al abrir la app aparece el **asistente de configuración inicial**
2. Define el horario (por defecto **02:00 – 07:00**)
3. Se instalan **4 mensajes preconfigurados** (diabetes, glaucoma, descanso, cariño)
4. Crea una **contraseña de administrador** (solo tú la conocerás)
5. Activa inicio con Windows y acceso directo en escritorio

## Mensajes preconfigurados

Al instalar por primera vez se cargan 4 mensajes en **modo presentación** (rotan cada 30 segundos):

1. **A esta hora no conviene comer** — hambre nocturna, pan duro, diabetes, alimentos del día
2. **Tus ojos necesitan descanso** — glaucoma, luz de pantalla, ciclo circadiano
3. **Es hora de descansar** — no usar la PC de madrugada
4. **Te queremos y te cuidamos** — tono afectuoso, sin castigo

Puedes editarlos, activar/desactivar cada uno, o agregar más desde el panel de administración.

## Uso diario

| Acción | Cómo |
|--------|------|
| Abrir panel admin | Escritorio o bandeja → contraseña |
| Probar el bloqueo | Panel o bandeja → **Probar bloqueo ahora** |
| Desbloquear temporalmente | Botón discreto en pantalla de bloqueo → contraseña |
| Reactivar bloqueo automático | Bandeja → **Reactivar bloqueo automático** |
| Cerrar la app | Bandeja → **Salir** (pide contraseña) |

## Personalizar mensajes

Puedes guardar **varios mensajes** y elegir cuáles mostrar (checkbox por mensaje).

### Modos de visualización

- **Un solo mensaje**: muestra el primero que tengas marcado como activo
- **Presentación**: rota entre todos los mensajes activos cada **N segundos** (configurable entre 5 y 600), con transición suave

Variables en el texto:

- `{hora}` → hora actual (ej. 02:15)
- `{fecha}` → fecha en español (ej. martes 21 de julio)

## Dónde guarda la configuración

`%AppData%\BlockMachine\config.json`

## Limitaciones del MVP

- Bloquea con pantalla completa encima de todo; no impide apagar la PC con el botón físico
- Ctrl+Alt+Supr sigue funcionando (limitación de Windows)
- Recomendado combinar con medidas físicas (guardar teclado/ratón de noche) si hace falta

## Desarrollo

```bash
dotnet build
dotnet run --project src/BlockMachine
dotnet run --project src/BlockMachine -- --admin
```
