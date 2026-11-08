using System;
using System.Collections.Generic;

namespace IStream
{
    // Tłumaczenia interfejsu. Kolejność: en, pl, de, es, fr, uk.
    static class L
    {
        public static readonly string[] Codes = { "en", "pl", "de", "es", "fr", "uk" };
        public static readonly string[] Names = { "English", "Polski", "Deutsch", "Español", "Français", "Українська" };
        public static string Lang = "en";

        static readonly Dictionary<string, string[]> map = new Dictionary<string, string[]>();

        static void A(string key, params string[] v) { map[key] = v; }

        public static string T(string key)
        {
            string[] v;
            if (!map.TryGetValue(key, out v)) return key;
            int i = Array.IndexOf(Codes, Lang);
            if (i < 0 || i >= v.Length || string.IsNullOrEmpty(v[i])) i = 0;
            return v[i];
        }

        public static string F(string key, params object[] args) { return string.Format(T(key), args); }

        static L()
        {
            A("nav_stream", "Stream", "Stream", "Stream", "Transmisión", "Diffusion", "Трансляція");
            A("nav_settings", "Settings", "Ustawienia", "Einstellungen", "Ajustes", "Paramètres", "Налаштування");
            A("nav_help", "Help", "Pomoc", "Hilfe", "Ayuda", "Aide", "Довідка");

            A("status_stopped", "Server stopped", "Serwer zatrzymany", "Server gestoppt", "Servidor detenido", "Serveur arrêté", "Сервер зупинено");
            A("status_starting", "Starting…", "Uruchamianie…", "Wird gestartet…", "Iniciando…", "Démarrage…", "Запуск…");
            A("status_stopping", "Stopping…", "Zatrzymywanie…", "Wird gestoppt…", "Deteniendo…", "Arrêt…", "Зупинка…");
            A("status_ready", "Ready to stream", "Gotowy do streamu", "Bereit zum Streamen", "Listo para transmitir", "Prêt à diffuser", "Готово до трансляції");
            A("status_streaming", "Streaming", "Trwa stream", "Stream läuft", "Transmitiendo", "Diffusion en cours", "Трансляція триває");

            A("s_stopped", "Stopped", "Zatrzymany", "Gestoppt", "Detenido", "Arrêté", "Зупинено");
            A("s_ready", "Ready", "Gotowy", "Bereit", "Listo", "Prêt", "Готово");
            A("s_streaming", "Streaming", "Stream", "Stream läuft", "Transmitiendo", "Diffusion", "Трансляція");
            A("s_busy", "Please wait…", "Chwila…", "Bitte warten…", "Espera…", "Patientez…", "Зачекайте…");

            A("hint_stopped",
              "Start the server so your iPhone can find this PC.",
              "Uruchom serwer, aby iPhone znalazł ten komputer.",
              "Starte den Server, damit dein iPhone diesen PC findet.",
              "Inicia el servidor para que tu iPhone encuentre este PC.",
              "Démarrez le serveur pour que votre iPhone trouve ce PC.",
              "Запустіть сервер, щоб iPhone знайшов цей комп'ютер.");
            A("hint_ready",
              "On your iPhone open Control Center → Screen Mirroring → {0}. Both devices must be on the same network.",
              "Na iPhonie otwórz Centrum sterowania → Klonowanie ekranu → {0}. Oba urządzenia muszą być w tej samej sieci.",
              "Öffne auf dem iPhone das Kontrollzentrum → Bildschirmsynchronisierung → {0}. Beide Geräte müssen im selben Netzwerk sein.",
              "En el iPhone, abre el Centro de control → Duplicar pantalla → {0}. Ambos dispositivos deben estar en la misma red.",
              "Sur l'iPhone, ouvrez le Centre de contrôle → Recopie de l'écran → {0}. Les deux appareils doivent être sur le même réseau.",
              "На iPhone відкрийте Пункт керування → Дзеркалювання екрана → {0}. Обидва пристрої мають бути в одній мережі.");
            A("hint_streaming",
              "Ctrl+Alt+F toggles fullscreen. Stop mirroring on your iPhone to end.",
              "Ctrl+Alt+F przełącza pełny ekran. Aby zakończyć, wyłącz klonowanie na iPhonie.",
              "Strg+Alt+F schaltet den Vollbildmodus um. Beende die Synchronisierung am iPhone, um aufzuhören.",
              "Ctrl+Alt+F alterna la pantalla completa. Para terminar, detén la duplicación en el iPhone.",
              "Ctrl+Alt+F bascule le plein écran. Arrêtez la recopie sur l'iPhone pour terminer.",
              "Ctrl+Alt+F — повноекранний режим. Щоб завершити, вимкніть дзеркалювання на iPhone.");
            A("hint_connect_first",
              "Connect your iPhone first — fullscreen works on the video window.",
              "Najpierw połącz iPhone'a — pełny ekran działa na oknie z obrazem.",
              "Verbinde zuerst dein iPhone – das Vollbild gilt für das Videofenster.",
              "Primero conecta tu iPhone: la pantalla completa se aplica a la ventana de vídeo.",
              "Connectez d'abord votre iPhone — le plein écran s'applique à la fenêtre vidéo.",
              "Спершу підключіть iPhone — повний екран працює з вікном відео.");
            A("hint_reconnect",
              "Server restarted with the new settings — reconnect your iPhone.",
              "Serwer uruchomiony ponownie z nowymi ustawieniami — połącz iPhone'a ponownie.",
              "Server mit neuen Einstellungen neu gestartet – verbinde dein iPhone erneut.",
              "Servidor reiniciado con los nuevos ajustes: vuelve a conectar tu iPhone.",
              "Serveur redémarré avec les nouveaux paramètres — reconnectez votre iPhone.",
              "Сервер перезапущено з новими налаштуваннями — знову підключіть iPhone.");
            A("hint_fw_done",
              "Firewall fixed. You can connect your iPhone.",
              "Zapora naprawiona. Możesz łączyć iPhone'a.",
              "Firewall repariert. Du kannst dein iPhone verbinden.",
              "Firewall reparado. Ya puedes conectar tu iPhone.",
              "Pare-feu réparé. Vous pouvez connecter votre iPhone.",
              "Брандмауер виправлено. Можна підключати iPhone.");

            A("btn_start", "Start server", "Uruchom serwer", "Server starten", "Iniciar servidor", "Démarrer le serveur", "Запустити сервер");
            A("btn_stop", "Stop server", "Zatrzymaj serwer", "Server stoppen", "Detener servidor", "Arrêter le serveur", "Зупинити сервер");
            A("btn_fullscreen", "Fullscreen", "Pełny ekran", "Vollbild", "Pantalla completa", "Plein écran", "Повний екран");
            A("btn_exit_fullscreen", "Exit fullscreen", "Wyjdź z pełnego ekranu", "Vollbild beenden", "Salir de pantalla completa", "Quitter le plein écran", "Вийти з повного екрана");

            A("rot_title", "Rotation", "Obrót", "Drehung", "Rotación", "Rotation", "Поворот");
            A("rot_0", "Original (0°)", "Oryginalnie (0°)", "Original (0°)", "Original (0°)", "Original (0°)", "Без повороту (0°)");
            A("rot_1", "Rotate right 90°", "Obróć w prawo o 90°", "90° nach rechts drehen", "Girar 90° a la derecha", "Pivoter de 90° vers la droite", "Повернути праворуч на 90°");
            A("rot_2", "Upside down (180°)", "Do góry nogami (180°)", "Auf dem Kopf (180°)", "Al revés (180°)", "À l'envers (180°)", "Догори дном (180°)");
            A("rot_3", "Rotate left 90°", "Obróć w lewo o 90°", "90° nach links drehen", "Girar 90° a la izquierda", "Pivoter de 90° vers la gauche", "Повернути ліворуч на 90°");

            A("setup_title", "Current setup", "Bieżące ustawienia", "Aktuelle Einstellungen", "Configuración actual", "Configuration actuelle", "Поточні налаштування");
            A("chip_lowlat", "Low latency", "Niskie opóźnienie", "Niedrige Latenz", "Baja latencia", "Faible latence", "Низька затримка");
            A("chip_noaudio", "Audio off", "Bez dźwięku", "Ton aus", "Sin audio", "Son coupé", "Без звуку");

            A("pending_title",
              "Changes saved — they'll apply on the next connection",
              "Zmiany zapisane — zadziałają przy następnym połączeniu",
              "Änderungen gespeichert – sie gelten ab der nächsten Verbindung",
              "Cambios guardados: se aplicarán en la próxima conexión",
              "Modifications enregistrées — appliquées à la prochaine connexion",
              "Зміни збережено — застосуються під час наступного підключення");
            A("pending_body",
              "Your stream keeps running. Resolution, frame rate and rotation are agreed with the iPhone when it connects.",
              "Stream trwa dalej. Rozdzielczość, liczba klatek i obrót są ustalane z iPhone'em w chwili połączenia.",
              "Dein Stream läuft weiter. Auflösung, Bildrate und Drehung werden beim Verbinden mit dem iPhone festgelegt.",
              "La transmisión continúa. La resolución, los fotogramas y la rotación se acuerdan con el iPhone al conectarse.",
              "La diffusion continue. La résolution, la fréquence d'images et la rotation sont fixées avec l'iPhone à la connexion.",
              "Трансляція триває. Роздільність, частота кадрів і поворот узгоджуються з iPhone під час підключення.");
            A("apply_now", "Apply now", "Zastosuj teraz", "Jetzt anwenden", "Aplicar ahora", "Appliquer maintenant", "Застосувати зараз");
            A("apply_now_tip",
              "Restarts the server — reconnect your iPhone afterwards.",
              "Restartuje serwer — potem połącz iPhone'a ponownie.",
              "Startet den Server neu – verbinde danach dein iPhone erneut.",
              "Reinicia el servidor; después vuelve a conectar el iPhone.",
              "Redémarre le serveur — reconnectez ensuite votre iPhone.",
              "Перезапускає сервер — потім знову підключіть iPhone.");

            A("fw_blocked",
              "Windows Firewall is blocking iStream — your iPhone can't connect.",
              "Zapora Windows blokuje iStream — iPhone nie może się połączyć.",
              "Die Windows-Firewall blockiert iStream – dein iPhone kann sich nicht verbinden.",
              "El Firewall de Windows está bloqueando iStream: tu iPhone no puede conectarse.",
              "Le pare-feu Windows bloque iStream — votre iPhone ne peut pas se connecter.",
              "Брандмауер Windows блокує iStream — iPhone не може підключитися.");
            A("fw_norule",
              "Windows Firewall hasn't allowed iStream yet — connections may be blocked.",
              "Zapora Windows nie zezwoliła jeszcze na iStream — połączenia mogą być blokowane.",
              "Die Windows-Firewall hat iStream noch nicht zugelassen – Verbindungen könnten blockiert werden.",
              "El Firewall de Windows aún no ha permitido iStream: las conexiones podrían bloquearse.",
              "Le pare-feu Windows n'a pas encore autorisé iStream — les connexions peuvent être bloquées.",
              "Брандмауер Windows ще не дозволив iStream — з'єднання можуть блокуватися.");
            A("fw_fix", "Fix firewall", "Napraw zaporę", "Firewall reparieren", "Reparar firewall", "Réparer le pare-feu", "Виправити брандмауер");
            A("fw_fixing", "Fixing…", "Naprawiam…", "Wird repariert…", "Reparando…", "Réparation…", "Виправлення…");
            A("fw_confirm",
              "iStream will add a Windows Firewall rule that lets devices on your network connect to the AirPlay receiver (engine\\uxplay-windows.exe), and remove rules that block it.\n\nThe rule applies to all network types, including public ones.\nWindows will ask for administrator approval. Continue?",
              "iStream doda regułę Zapory Windows, która pozwala urządzeniom w Twojej sieci łączyć się z odbiornikiem AirPlay (engine\\uxplay-windows.exe), i usunie reguły, które go blokują.\n\nReguła działa we wszystkich typach sieci, także publicznych.\nWindows poprosi o zgodę administratora. Kontynuować?",
              "iStream fügt eine Windows-Firewall-Regel hinzu, die Geräten in deinem Netzwerk die Verbindung zum AirPlay-Empfänger (engine\\uxplay-windows.exe) erlaubt, und entfernt Regeln, die ihn blockieren.\n\nDie Regel gilt für alle Netzwerktypen, auch öffentliche.\nWindows fragt nach Administratorrechten. Fortfahren?",
              "iStream añadirá una regla al Firewall de Windows que permite a los dispositivos de tu red conectarse al receptor AirPlay (engine\\uxplay-windows.exe) y eliminará las reglas que lo bloquean.\n\nLa regla se aplica a todos los tipos de red, incluidas las públicas.\nWindows pedirá permiso de administrador. ¿Continuar?",
              "iStream va ajouter une règle au pare-feu Windows qui permet aux appareils de votre réseau de se connecter au récepteur AirPlay (engine\\uxplay-windows.exe), et supprimer les règles qui le bloquent.\n\nLa règle s'applique à tous les types de réseaux, y compris publics.\nWindows demandera l'autorisation d'un administrateur. Continuer ?",
              "iStream додасть правило брандмауера Windows, яке дозволяє пристроям у вашій мережі підключатися до приймача AirPlay (engine\\uxplay-windows.exe), і видалить правила, що його блокують.\n\nПравило діє для всіх типів мереж, зокрема загальнодоступних.\nWindows попросить дозвіл адміністратора. Продовжити?");
            A("fw_failed",
              "Firewall settings were not changed (the administrator prompt was cancelled or an error occurred).",
              "Nie zmieniono ustawień zapory (anulowano okno administratora albo wystąpił błąd).",
              "Die Firewall-Einstellungen wurden nicht geändert (Administratorabfrage abgebrochen oder Fehler).",
              "No se cambió la configuración del firewall (se canceló la solicitud de administrador o hubo un error).",
              "Les paramètres du pare-feu n'ont pas été modifiés (demande d'administrateur annulée ou erreur).",
              "Налаштування брандмауера не змінено (запит адміністратора скасовано або сталася помилка).");

            A("sec_quality", "Stream quality", "Jakość streamu", "Stream-Qualität", "Calidad de transmisión", "Qualité de diffusion", "Якість трансляції");
            A("sec_playback", "Playback", "Odtwarzanie", "Wiedergabe", "Reproducción", "Lecture", "Відтворення");
            A("sec_general", "General", "Ogólne", "Allgemein", "General", "Général", "Загальні");

            A("lbl_resolution", "Resolution", "Rozdzielczość", "Auflösung", "Resolución", "Résolution", "Роздільність");
            A("desc_resolution",
              "Higher is sharper but needs a stronger network.",
              "Wyższa daje ostrzejszy obraz, ale wymaga lepszej sieci.",
              "Höher ist schärfer, braucht aber ein stärkeres Netzwerk.",
              "Más alta es más nítida, pero necesita una red mejor.",
              "Plus élevée = plus net, mais demande un meilleur réseau.",
              "Вища — чіткіше, але потрібна краща мережа.");
            A("q0", "720p — lowest latency", "720p — najmniejsze opóźnienie", "720p — geringste Latenz", "720p — mínima latencia", "720p — latence minimale", "720p — найменша затримка");
            A("q1", "1080p — recommended", "1080p — zalecane", "1080p — empfohlen", "1080p — recomendado", "1080p — recommandé", "1080p — рекомендовано");
            A("q2", "1440p — high quality", "1440p — wysoka jakość", "1440p — hohe Qualität", "1440p — alta calidad", "1440p — haute qualité", "1440p — висока якість");
            A("q3", "4K (H.265) — maximum", "4K (H.265) — maksymalna", "4K (H.265) — maximal", "4K (H.265) — máxima", "4K (H.265) — maximale", "4K (H.265) — максимальна");

            A("lbl_fps", "Frame rate", "Klatki na sekundę", "Bildrate", "Fotogramas por segundo", "Images par seconde", "Частота кадрів");
            A("desc_fps",
              "60 FPS is smoothest; 30 FPS uses less bandwidth.",
              "60 FPS jest najpłynniejsze, 30 FPS mniej obciąża sieć.",
              "60 FPS ist am flüssigsten, 30 FPS braucht weniger Bandbreite.",
              "60 FPS es lo más fluido; 30 FPS usa menos ancho de banda.",
              "60 FPS est le plus fluide ; 30 FPS consomme moins de bande passante.",
              "60 FPS — найплавніше, 30 FPS — менше навантаження на мережу.");
            A("fps60", "60 FPS — smoothest", "60 FPS — najpłynniej", "60 FPS — am flüssigsten", "60 FPS — lo más fluido", "60 FPS — le plus fluide", "60 FPS — найплавніше");
            A("fps30", "30 FPS — lighter", "30 FPS — lżej dla sieci", "30 FPS — sparsamer", "30 FPS — más ligero", "30 FPS — plus léger", "30 FPS — легше для мережі");

            A("lbl_renderer", "GPU renderer", "Renderer GPU", "GPU-Renderer", "Renderizador GPU", "Rendu GPU", "Графічний рендерер");
            A("desc_renderer",
              "Direct3D 11 works best on most PCs.",
              "Direct3D 11 działa najlepiej na większości komputerów.",
              "Direct3D 11 funktioniert auf den meisten PCs am besten.",
              "Direct3D 11 funciona mejor en la mayoría de los PC.",
              "Direct3D 11 fonctionne le mieux sur la plupart des PC.",
              "Direct3D 11 найкраще працює на більшості ПК.");
            A("r0", "Direct3D 11 — recommended", "Direct3D 11 — zalecany", "Direct3D 11 — empfohlen", "Direct3D 11 — recomendado", "Direct3D 11 — recommandé", "Direct3D 11 — рекомендовано");

            A("lbl_lowlat", "Low-latency mode", "Tryb niskiego opóźnienia", "Modus mit niedriger Latenz", "Modo de baja latencia", "Mode faible latence", "Режим низької затримки");
            A("desc_lowlat",
              "Shows each frame immediately without waiting for audio sync.",
              "Każda klatka jest pokazywana od razu, bez czekania na synchronizację z dźwiękiem.",
              "Zeigt jedes Bild sofort, ohne auf die Audiosynchronisation zu warten.",
              "Muestra cada fotograma al instante, sin esperar la sincronización del audio.",
              "Affiche chaque image immédiatement, sans attendre la synchronisation audio.",
              "Кожен кадр показується одразу, без очікування синхронізації зі звуком.");
            A("lbl_audio", "Play phone audio on this PC", "Dźwięk z telefonu na komputerze", "Telefonton auf diesem PC", "Audio del teléfono en este PC", "Son du téléphone sur ce PC", "Звук телефона на цьому ПК");
            A("desc_audio",
              "Turn off to keep sound on the iPhone.",
              "Wyłącz, żeby dźwięk został na iPhonie.",
              "Ausschalten, damit der Ton auf dem iPhone bleibt.",
              "Desactívalo para mantener el sonido en el iPhone.",
              "Désactivez pour garder le son sur l'iPhone.",
              "Вимкніть, щоб звук лишався на iPhone.");
            A("lbl_autofs", "Fullscreen on connect", "Pełny ekran po połączeniu", "Vollbild beim Verbinden", "Pantalla completa al conectar", "Plein écran à la connexion", "Повний екран при підключенні");
            A("desc_autofs",
              "Video goes fullscreen as soon as your iPhone connects.",
              "Obraz przechodzi na pełny ekran, gdy tylko iPhone się połączy.",
              "Das Video wechselt in den Vollbildmodus, sobald das iPhone verbunden ist.",
              "El vídeo pasa a pantalla completa en cuanto se conecta el iPhone.",
              "La vidéo passe en plein écran dès que l'iPhone se connecte.",
              "Відео розгортається на весь екран, щойно iPhone підключиться.");
            A("lbl_monitor", "Fullscreen monitor", "Monitor pełnego ekranu", "Bildschirm für Vollbild", "Monitor de pantalla completa", "Écran du plein écran", "Монітор для повного екрана");
            A("desc_monitor",
              "Where the video goes in fullscreen. Automatic uses the screen the window is on.",
              "Gdzie wyświetlać obraz na pełnym ekranie. „Automatycznie” = ekran, na którym jest okno.",
              "Wo das Video im Vollbild angezeigt wird. „Automatisch“ nutzt den Bildschirm des Fensters.",
              "Dónde se muestra el vídeo en pantalla completa. «Automático» usa la pantalla de la ventana.",
              "Où afficher la vidéo en plein écran. « Automatique » utilise l'écran de la fenêtre.",
              "Де показувати відео на весь екран. «Автоматично» — екран, на якому вікно.");
            A("mon_auto", "Automatic", "Automatycznie", "Automatisch", "Automático", "Automatique", "Автоматично");
            A("mon_n", "Monitor {0} — {1}×{2}", "Monitor {0} — {1}×{2}", "Bildschirm {0} — {1}×{2}", "Monitor {0} — {1}×{2}", "Écran {0} — {1}×{2}", "Монітор {0} — {1}×{2}");
            A("mon_primary", " (primary)", " (główny)", " (Hauptbildschirm)", " (principal)", " (principal)", " (основний)");

            A("lbl_name", "Name in AirPlay list", "Nazwa na liście AirPlay", "Name in der AirPlay-Liste", "Nombre en la lista AirPlay", "Nom dans la liste AirPlay", "Назва у списку AirPlay");
            A("desc_name",
              "How this PC appears on your iPhone. Press Enter to save.",
              "Tak ten komputer będzie widoczny na iPhonie. Enter zapisuje.",
              "So erscheint dieser PC auf deinem iPhone. Mit Enter speichern.",
              "Así aparece este PC en tu iPhone. Pulsa Intro para guardar.",
              "Nom de ce PC sur votre iPhone. Appuyez sur Entrée pour enregistrer.",
              "Так цей ПК відображатиметься на iPhone. Enter — зберегти.");
            A("lbl_lang", "Language", "Język", "Sprache", "Idioma", "Langue", "Мова");
            A("desc_lang", "Language of the iStream interface.", "Język interfejsu iStream.", "Sprache der iStream-Oberfläche.", "Idioma de la interfaz de iStream.", "Langue de l'interface d'iStream.", "Мова інтерфейсу iStream.");
            A("lbl_theme", "Appearance", "Wygląd", "Erscheinungsbild", "Apariencia", "Apparence", "Вигляд");
            A("desc_theme",
              "Follow Windows, or always light, gray or dark.",
              "Jak w Windows albo zawsze jasny, szary lub ciemny.",
              "Wie Windows oder immer hell, grau bzw. dunkel.",
              "Igual que Windows, o siempre claro, gris u oscuro.",
              "Comme Windows, ou toujours clair, gris ou sombre.",
              "Як у Windows або завжди світлий, сірий чи темний.");
            A("th0", "Same as Windows", "Jak w Windows", "Wie Windows", "Igual que Windows", "Comme Windows", "Як у Windows");
            A("th1", "Light", "Jasny", "Hell", "Claro", "Clair", "Світлий");
            A("th2", "Dark", "Ciemny", "Dunkel", "Oscuro", "Sombre", "Темний");
            A("th3", "Gray", "Szary", "Grau", "Gris", "Gris", "Сірий");
            A("lbl_background", "Keep running in the background", "Działaj w tle po zamknięciu", "Im Hintergrund weiterlaufen", "Seguir en segundo plano", "Continuer en arrière-plan", "Працювати у фоні");
            A("desc_background",
              "Closing the window keeps iStream in the tray, so your iPhone can connect any time.",
              "Zamknięcie okna zostawia iStream w zasobniku, więc iPhone może się połączyć w każdej chwili.",
              "Beim Schließen bleibt iStream im Infobereich, damit sich dein iPhone jederzeit verbinden kann.",
              "Al cerrar la ventana, iStream sigue en la bandeja y tu iPhone puede conectarse en cualquier momento.",
              "Fermer la fenêtre laisse iStream dans la zone de notification : votre iPhone peut se connecter à tout moment.",
              "Закриття вікна залишає iStream у треї, тож iPhone може під’єднатися будь-коли.");
            A("tray_open", "Open iStream", "Otwórz iStream", "iStream öffnen", "Abrir iStream", "Ouvrir iStream", "Відкрити iStream");
            A("tray_quit", "Quit iStream", "Zamknij iStream", "iStream beenden", "Salir de iStream", "Quitter iStream", "Вийти з iStream");
            A("tray_hint_t", "iStream is still running", "iStream nadal działa", "iStream läuft weiter", "iStream sigue funcionando", "iStream fonctionne toujours", "iStream працює далі");
            A("tray_hint_b",
              "Your iPhone can mirror to this PC any time. To quit, right-click the iStream icon here.",
              "iPhone może w każdej chwili klonować ekran na ten komputer. Aby zamknąć, kliknij tu prawym przyciskiem ikonę iStream.",
              "Dein iPhone kann jederzeit auf diesen PC spiegeln. Zum Beenden hier mit der rechten Maustaste auf das iStream-Symbol klicken.",
              "Tu iPhone puede duplicar la pantalla en este PC en cualquier momento. Para salir, haz clic derecho aquí en el icono de iStream.",
              "Votre iPhone peut diffuser vers ce PC à tout moment. Pour quitter, faites un clic droit ici sur l'icône iStream.",
              "iPhone може будь-коли дублювати екран на цей ПК. Щоб вийти, клацніть тут правою кнопкою на значку iStream.");
            A("lbl_autostart", "Start server with the app", "Uruchamiaj serwer z aplikacją", "Server mit der App starten", "Iniciar el servidor con la aplicación", "Démarrer le serveur avec l'application", "Запускати сервер разом із програмою");
            A("desc_autostart",
              "iStream is ready as soon as you open it.",
              "iStream jest gotowy od razu po otwarciu.",
              "iStream ist sofort nach dem Öffnen bereit.",
              "iStream está listo en cuanto lo abres.",
              "iStream est prêt dès son ouverture.",
              "iStream готовий одразу після відкриття.");
            A("lbl_winstart", "Launch at Windows sign-in", "Uruchamiaj przy logowaniu do Windows", "Bei der Windows-Anmeldung starten", "Abrir al iniciar sesión en Windows", "Lancer à l'ouverture de session", "Запускати під час входу в Windows");
            A("desc_winstart",
              "Starts iStream quietly in the tray when you sign in.",
              "iStream uruchomi się po cichu w zasobniku po zalogowaniu.",
              "iStream startet nach der Anmeldung unauffällig im Infobereich.",
              "iStream se inicia discretamente en la bandeja al iniciar sesión.",
              "iStream démarre discrètement dans la zone de notification à la connexion.",
              "iStream тихо запускатиметься в треї після входу.");

            A("help_connect_t", "Connecting", "Łączenie", "Verbinden", "Conexión", "Connexion", "Підключення");
            A("help_connect_b",
              "1.  Make sure the status says “Ready to stream”.\n2.  On your iPhone open Control Center → Screen Mirroring.\n3.  Choose “{0}”.",
              "1.  Upewnij się, że status to „Gotowy do streamu”.\n2.  Na iPhonie otwórz Centrum sterowania → Klonowanie ekranu.\n3.  Wybierz „{0}”.",
              "1.  Prüfe, dass der Status „Bereit zum Streamen“ lautet.\n2.  Öffne auf dem iPhone das Kontrollzentrum → Bildschirmsynchronisierung.\n3.  Wähle „{0}“.",
              "1.  Comprueba que el estado diga «Listo para transmitir».\n2.  En el iPhone, abre el Centro de control → Duplicar pantalla.\n3.  Elige «{0}».",
              "1.  Vérifiez que l'état indique « Prêt à diffuser ».\n2.  Sur l'iPhone, ouvrez le Centre de contrôle → Recopie de l'écran.\n3.  Choisissez « {0} ».",
              "1.  Переконайтеся, що статус — «Готово до трансляції».\n2.  На iPhone відкрийте Пункт керування → Дзеркалювання екрана.\n3.  Виберіть «{0}».");
            A("help_shortcuts_t", "Keyboard shortcuts", "Skróty klawiszowe", "Tastenkürzel", "Atajos de teclado", "Raccourcis clavier", "Сполучення клавіш");
            A("help_shortcuts_b",
              "Ctrl+Alt+F  —  toggle fullscreen\nCtrl+Alt+R  —  rotate 90°",
              "Ctrl+Alt+F  —  pełny ekran\nCtrl+Alt+R  —  obrót o 90°",
              "Strg+Alt+F  —  Vollbild\nStrg+Alt+R  —  um 90° drehen",
              "Ctrl+Alt+F  —  pantalla completa\nCtrl+Alt+R  —  girar 90°",
              "Ctrl+Alt+F  —  plein écran\nCtrl+Alt+R  —  rotation de 90°",
              "Ctrl+Alt+F  —  повний екран\nCtrl+Alt+R  —  поворот на 90°");
            A("help_trouble_t", "iPhone can't find this PC", "iPhone nie widzi komputera", "Das iPhone findet diesen PC nicht", "El iPhone no encuentra este PC", "L'iPhone ne trouve pas ce PC", "iPhone не бачить цей ПК");
            A("help_trouble_b",
              "•  Fix the firewall if iStream asks you to.\n•  Turn Wi‑Fi off and on on the iPhone.\n•  Stop and start the server.\n•  Restart the Bonjour service (Task Manager → Services).",
              "•  Napraw zaporę, jeśli iStream o to prosi.\n•  Wyłącz i włącz Wi‑Fi na iPhonie.\n•  Zatrzymaj i ponownie uruchom serwer.\n•  Zrestartuj usługę Bonjour (Menedżer zadań → Usługi).",
              "•  Repariere die Firewall, wenn iStream dazu auffordert.\n•  Schalte das WLAN am iPhone aus und wieder ein.\n•  Stoppe den Server und starte ihn neu.\n•  Starte den Bonjour-Dienst neu (Task-Manager → Dienste).",
              "•  Repara el firewall si iStream te lo pide.\n•  Apaga y enciende el Wi‑Fi del iPhone.\n•  Detén y vuelve a iniciar el servidor.\n•  Reinicia el servicio Bonjour (Administrador de tareas → Servicios).",
              "•  Réparez le pare-feu si iStream vous le demande.\n•  Désactivez puis réactivez le Wi‑Fi de l'iPhone.\n•  Arrêtez puis redémarrez le serveur.\n•  Redémarrez le service Bonjour (Gestionnaire des tâches → Services).",
              "•  Виправте брандмауер, якщо iStream про це просить.\n•  Вимкніть і ввімкніть Wi‑Fi на iPhone.\n•  Зупиніть і знову запустіть сервер.\n•  Перезапустіть службу Bonjour (Диспетчер завдань → Служби).");
            A("help_about_t", "About", "O programie", "Über", "Acerca de", "À propos", "Про програму");
            A("help_about_b",
              "iStream uses the open-source UxPlay AirPlay engine (GPL-3.0). Video is decoded, rotated and displayed entirely on the GPU.",
              "iStream korzysta z otwartego silnika AirPlay UxPlay (GPL-3.0). Obraz jest dekodowany, obracany i wyświetlany w całości na karcie graficznej.",
              "iStream nutzt die Open-Source-AirPlay-Engine UxPlay (GPL-3.0). Das Video wird vollständig auf der GPU dekodiert, gedreht und angezeigt.",
              "iStream usa el motor AirPlay de código abierto UxPlay (GPL-3.0). El vídeo se decodifica, gira y muestra completamente en la GPU.",
              "iStream utilise le moteur AirPlay open source UxPlay (GPL-3.0). La vidéo est décodée, pivotée et affichée entièrement sur le GPU.",
              "iStream використовує відкритий рушій AirPlay UxPlay (GPL-3.0). Відео декодується, повертається й відображається повністю на відеокарті.");

            A("shortcut_busy",
              "Shortcut Ctrl+Alt+{0} is used by another app — use the buttons instead.",
              "Skrót Ctrl+Alt+{0} jest zajęty przez inny program — użyj przycisków.",
              "Das Kürzel Strg+Alt+{0} wird von einer anderen App verwendet – nutze die Schaltflächen.",
              "El atajo Ctrl+Alt+{0} lo usa otra aplicación: usa los botones.",
              "Le raccourci Ctrl+Alt+{0} est utilisé par une autre application — utilisez les boutons.",
              "Сполучення Ctrl+Alt+{0} зайняте іншою програмою — користуйтеся кнопками.");
            A("engine_missing_t", "AirPlay engine missing", "Brak silnika AirPlay", "AirPlay-Engine fehlt", "Falta el motor AirPlay", "Moteur AirPlay introuvable", "Не знайдено рушій AirPlay");
            A("engine_missing_b",
              "engine\\uxplay-windows.exe was not found next to iStream.exe.",
              "Nie znaleziono pliku engine\\uxplay-windows.exe obok iStream.exe.",
              "engine\\uxplay-windows.exe wurde neben iStream.exe nicht gefunden.",
              "No se encontró engine\\uxplay-windows.exe junto a iStream.exe.",
              "Le fichier engine\\uxplay-windows.exe est introuvable à côté d'iStream.exe.",
              "Поруч з iStream.exe немає файлу engine\\uxplay-windows.exe.");
            A("err_start", "Couldn't start the server:", "Nie udało się uruchomić serwera:", "Der Server konnte nicht gestartet werden:", "No se pudo iniciar el servidor:", "Impossible de démarrer le serveur :", "Не вдалося запустити сервер:");
            A("machine_override",
              "Found {0}.\nIt overrides iStream's settings — delete it so your settings take effect.",
              "Znaleziono plik {0}.\nMa on pierwszeństwo przed ustawieniami iStream — usuń go, aby Twoje ustawienia działały.",
              "{0} wurde gefunden.\nDiese Datei überschreibt die Einstellungen von iStream – lösche sie, damit deine Einstellungen gelten.",
              "Se encontró {0}.\nTiene prioridad sobre los ajustes de iStream; elimínalo para que tus ajustes surtan efecto.",
              "{0} a été trouvé.\nIl remplace les paramètres d'iStream — supprimez-le pour que vos paramètres s'appliquent.",
              "Знайдено {0}.\nВін має пріоритет над налаштуваннями iStream — видаліть його, щоб ваші налаштування діяли.");
        }
    }
}
