using BlockMachine.Models;

namespace BlockMachine.Services;

public static class MessageService
{
    public static void Normalize(AppConfig config)
    {
        if (config.Messages.Count == 0 &&
            !string.IsNullOrWhiteSpace(config.Message))
        {
            config.Messages.Add(new BlockMessage
            {
                Title = config.Title,
                Body = config.Message,
                IsEnabled = true
            });
        }

        if (config.Messages.Count == 0)
        {
            config.Messages.AddRange(CreateDefaultMessages());
            config.DisplayMode = MessageDisplayMode.Slideshow;
        }

        if (config.SlideIntervalSeconds < 5)
        {
            config.SlideIntervalSeconds = 30;
        }
        else
        {
            config.SlideIntervalSeconds = ClampSlideInterval(config.SlideIntervalSeconds);
        }

        SyncLegacyFields(config);
    }

    public static List<BlockMessage> GetEnabledMessages(AppConfig config)
    {
        Normalize(config);
        return config.Messages
            .Where(message => message.IsEnabled && !string.IsNullOrWhiteSpace(message.Body))
            .ToList();
    }

    public static string FormatText(string template, DateTime? now = null)
    {
        var current = now ?? DateTime.Now;
        return template
            .Replace("{hora}", current.ToString("HH:mm"))
            .Replace("{fecha}", current.ToString("dddd d 'de' MMMM", new System.Globalization.CultureInfo("es-ES")));
    }

    public static bool HasValidMessages(IEnumerable<BlockMessage> messages)
    {
        return messages.Any(message => message.IsEnabled && !string.IsNullOrWhiteSpace(message.Body));
    }

    public static void SyncLegacyFields(AppConfig config)
    {
        var first = config.Messages.FirstOrDefault();
        if (first is null)
        {
            return;
        }

        config.Title = first.Title;
        config.Message = first.Body;
    }

    public static List<BlockMessage> CreateDefaultMessages()
    {
        return
        [
            new BlockMessage
            {
                Title = "A esta hora no conviene comer",
                Body =
                    "Son las {hora}.\n\n" +
                    "Sabemos que a esta hora te da hambre y buscas pan duro o algo rápido.\n" +
                    "Con tu diabetes, comer de madrugada te hace daño.\n\n" +
                    "Durante el día te cuesta tomar los alimentos que sí te convienen.\n" +
                    "Por favor, vuelve a la cama. Mañana preparamos contigo un desayuno adecuado.\n\n" +
                    "Lo hacemos porque te queremos.",
                IsEnabled = true
            },
            new BlockMessage
            {
                Title = "Tus ojos necesitan descanso",
                Body =
                    "Son las {hora}.\n\n" +
                    "Con tu glaucoma, caminar y usar la luz de la computadora de noche es contraproducente.\n" +
                    "Tu cuerpo necesita respetar el ciclo del sueño para cuidar tu vista y tu salud.\n\n" +
                    "Ahora toca descansar en la cama, con poca luz y en calma.\n" +
                    "Mañana, con el día, tendrás mejor luz y más seguridad para moverte.",
                IsEnabled = true
            },
            new BlockMessage
            {
                Title = "Es hora de descansar",
                Body =
                    "Son las {hora}.\n\n" +
                    "No es momento de usar la computadora.\n" +
                    "Tu cuerpo está pidiendo sueño, aunque a veces no lo sientas así.\n\n" +
                    "Vuelve a la cama. Estamos cerca y te cuidamos.\n" +
                    "Mañana tendrás todo el día para estar despierto.",
                IsEnabled = true
            },
            new BlockMessage
            {
                Title = "Te queremos y te cuidamos",
                Body =
                    "Sabemos que despertarte a esta hora es confuso y molesto.\n\n" +
                    "No estás haciendo nada mal. Solo necesitas descansar.\n" +
                    "Esta pantalla está aquí para protegerte, no para castigarte.\n\n" +
                    "Vuelve a la cama, papá. Te queremos mucho.",
                IsEnabled = true
            }
        ];
    }

    public static bool TryParseSlideInterval(string value, out int seconds)
    {
        seconds = 0;

        if (!int.TryParse(value?.Trim(), out var parsed))
        {
            return false;
        }

        if (parsed is < 5 or > 600)
        {
            return false;
        }

        seconds = parsed;
        return true;
    }

    public static int ClampSlideInterval(int seconds)
    {
        return Math.Clamp(seconds, 5, 600);
    }
}
