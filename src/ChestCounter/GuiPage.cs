namespace ChestCounter;

/// <summary>
/// The page of the graphical interface: one HTML file (styles and script inside), drawn by the script from the state
/// the counter sends (<see cref="GuiServer"/>). Texts come with the state, in the chosen language.
/// </summary>
public static class GuiPage
{
    /// <summary>Every text of the page, in the chosen language.</summary>
    public static Dictionary<string, string> Texts() => new()
    {
        ["home"] = L.T("Accueil", "Home", "Inicio"),
        ["report"] = L.T("Rapport", "Report", "Informe"),
        ["overlay"] = L.T("Overlay", "Overlay", "Overlay"),
        ["settings"] = L.T("Réglages", "Settings", "Ajustes"),
        ["help"] = L.T("Aide", "Help", "Ayuda"),
        ["total"] = L.T("Coffres au total", "Chests in total", "Cofres en total"),
        ["totalSub"] = L.T("ouverts ou détruits par toi", "opened or destroyed by you", "abiertos o destruidos por ti"),
        ["byHand"] = L.T("dont {0} ajoutés à la main", "{0} of them added by hand", "de ellos, {0} añadidos a mano"),
        ["session"] = L.T("Session", "Session", "Sesión"),
        ["last"] = L.T("Dernière partie", "Last match", "Última partida"),
        ["squad"] = L.T("Total d'équipe", "Squad total", "Total del equipo"),
        ["squadSub"] = L.T("toi + {0} par tes coéquipiers", "you + {0} by your teammates", "tú + {0} de tus compañeros"),
        ["inaccurate"] = L.T("imprécis", "inaccurate", "impreciso"),
        ["inaccurateWhy"] = L.T("Compté dans TES replays : un coffre ouvert par un coéquipier à plus de 150 m de toi n'y est pas, et rien n'est enregistré après ton départ de la partie. Le vrai total de l'équipe est plus haut.",
            "Counted in YOUR replays: a chest a teammate opens more than 150 m away from you is not in them, and nothing is recorded once you leave the match. The real squad total is higher.",
            "Contado en TUS repeticiones: un cofre que un compañero abre a más de 150 m de ti no está en ellas, y no se graba nada después de que salgas de la partida. El total real del equipo es mayor."),
        ["treasures"] = L.T("Trésors", "Treasures", "Tesoros"),
        ["matches"] = L.T("parties", "matches", "partidas"),
        ["perMatch"] = L.T("{0} par partie", "{0} per match", "{0} por partida"),
        ["openReport"] = L.T("Ouvrir le rapport", "Open the report", "Abrir el informe"),
        ["showOverlay"] = L.T("Afficher l'overlay", "Show the overlay", "Mostrar el overlay"),
        ["hideOverlay"] = L.T("Masquer l'overlay", "Hide the overlay", "Ocultar el overlay"),
        ["lock"] = L.T("Verrouiller l'overlay", "Lock the overlay", "Bloquear el overlay"),
        ["unlock"] = L.T("Déverrouiller pour le déplacer", "Unlock to move it", "Desbloquear para moverlo"),
        ["activity"] = L.T("Activité", "Activity", "Actividad"),
        ["noActivity"] = L.T("Rien pour l'instant.", "Nothing yet.", "Nada por ahora."),
        ["analyzing"] = L.T("Analyse de tes replays…", "Analyzing your replays…", "Analizando tus repeticiones…"),
        ["whatsNew"] = L.T("Nouveautés de la {0}", "What's new in {0}", "Novedades de la {0}"),
        ["newVersion"] = L.T("Nouvelle version {0} disponible", "New version {0} available", "Nueva versión {0} disponible"),
        ["install"] = L.T("Installer", "Install", "Instalar"),
        ["checkUpdate"] = L.T("Rechercher une mise à jour", "Check for updates", "Buscar actualizaciones"),
        ["welcome"] = L.T("Bienvenue !", "Welcome!", "¡Bienvenido!"),
        ["welcomeText"] = L.T("Active d'abord les rediffusions dans Fortnite : Paramètres → Jeu → Rediffusions → Enregistrer les rediffusions. Ensuite, laisse le compteur ouvert : chaque partie est comptée quelques secondes après sa fin. Des coffres ouverts avant ? Le crayon de « Coffres au total » permet de les ajouter.",
            "First turn on replays in Fortnite: Settings → Game → Replays → Record Replays. Then keep the counter open: each match is counted a few seconds after it ends. Chests opened before? The pencil of \"Chests in total\" adds them.",
            "Primero activa las repeticiones en Fortnite: Ajustes → Juego → Repeticiones → Grabar repeticiones. Luego deja el contador abierto: cada partida se cuenta unos segundos después de terminar. ¿Cofres abiertos antes? El lápiz de «Cofres en total» permite añadirlos."),
        ["ok"] = L.T("OK", "OK", "OK"),
        ["moreInfo"] = L.T("En savoir plus", "More info", "Más información"),
        ["wMin"] = L.T("Réduire", "Minimize", "Minimizar"),
        ["wMax"] = L.T("Agrandir", "Maximize", "Maximizar"),
        ["wRestore"] = L.T("Niveau inférieur", "Restore down", "Restaurar"),
        ["wClose"] = L.T("Fermer", "Close", "Cerrar"),
        ["keepOpen"] = L.T("Garder le compteur ouvert", "Keep the counter open", "Mantener el contador abierto"),
        ["autoCloseText"] = L.T("Fortnite est fermé : le compteur, ouvert avec Fortnite, va se fermer tout seul.", "Fortnite is closed: the counter, opened with Fortnite, is about to close by itself.", "Fortnite está cerrado: el contador, abierto con Fortnite, se va a cerrar solo."),
        ["welcomeQuick"] = L.T("Tes premiers réglages (tu les retrouves à tout moment dans Réglages) :", "Your first settings (you find them at any time in Settings):", "Tus primeros ajustes (los encuentras en cualquier momento en Ajustes):"),
        ["welcomeDone"] = L.T("C'est parti !", "Let's go!", "¡Vamos!"),
        ["inGame"] = L.T("Overlay en jeu", "In-game overlay", "Overlay en el juego"),
        ["inGameHelp"] = L.T("Petite fenêtre toujours au premier plan : ta chance d'avoir la boutique pirate, tes coffres et la partie en cours (Fortnite en « Plein écran fenêtré »). Ctrl+Maj+O : afficher / masquer, Ctrl+Maj+L : verrouiller.", "Small always-on-top window: your Pirate Shop chance, your chests and the match in progress (Fortnite in \"Windowed Fullscreen\"). Ctrl+Shift+O: show / hide, Ctrl+Shift+L: lock.", "Pequeña ventana siempre visible: tu probabilidad de la Tienda Pirata, tus cofres y la partida en curso (Fortnite en «Pantalla completa en ventana»). Ctrl+Mayús+O: mostrar / ocultar, Ctrl+Mayús+L: bloquear."),
        ["style"] = L.T("Style", "Style", "Estilo"),
        ["hud"] = L.T("HUD (chiffres seuls, intégré au jeu)", "HUD (figures only, blends into the game)", "HUD (solo cifras, integrado en el juego)"),
        ["classic"] = L.T("Carte compacte", "Compact card", "Tarjeta compacta"),
        ["size"] = L.T("Taille", "Size", "Tamaño"),
        ["opacity"] = L.T("Opacité", "Opacity", "Opacidad"),
        ["squadToggle"] = L.T("Afficher le total d'équipe", "Show the squad total", "Mostrar el total del equipo"),
        ["squadHelp"] = L.T("Tes coffres + ceux de tes coéquipiers, sur l'accueil et le rapport.", "Your chests + your teammates', on the home tab and the report.", "Tus cofres + los de tus compañeros, en el inicio y el informe."),
        ["obs"] = L.T("Overlay pour OBS (stream)", "Overlay for OBS (stream)", "Overlay para OBS (directo)"),
        ["obsSteps"] = L.T("Dans OBS : Sources → + → Navigateur, coche « Fichier local », choisis ce fichier. Largeur 300, hauteur 170 (280 × 150 pour la carte). Pour ne l'avoir que sur le stream, désactive l'overlay en jeu : la page continue d'être mise à jour.", "In OBS: Sources → + → Browser, tick \"Local file\", pick this file. Width 300, height 170 (280 × 150 for the card). To have it on the stream only, turn the in-game overlay off: the page keeps being updated.", "En OBS: Fuentes → + → Navegador, marca «Archivo local», elige este archivo. Ancho 300, alto 170 (280 × 150 para la tarjeta). Para tenerlo solo en el directo, desactiva el overlay en el juego: la página se sigue actualizando."),
        ["copy"] = L.T("Copier le chemin", "Copy the path", "Copiar la ruta"),
        ["copied"] = L.T("Copié !", "Copied!", "¡Copiado!"),
        ["preview"] = L.T("Aperçu", "Preview", "Vista previa"),
        ["language"] = L.T("Langue", "Language", "Idioma"),
        ["tray"] = L.T("Réduire dans la zone de notification", "Minimize to the notification area", "Minimizar al área de notificación"),
        ["trayHelp"] = L.T("Oui : réduire la fenêtre la range à côté de l'horloge, en bas à droite de la barre des tâches (icône du coffre, parfois derrière la petite flèche ^). Non : elle reste dans la barre des tâches comme une fenêtre normale. Dans les deux cas, le compteur continue de compter.",
            "Yes: minimizing the window puts it next to the clock, at the bottom right of the taskbar (chest icon, sometimes behind the small ^ arrow). No: it stays in the taskbar like a normal window. Either way, the counter keeps counting.",
            "Sí: minimizar la ventana la coloca junto al reloj, abajo a la derecha de la barra de tareas (icono del cofre, a veces detrás de la pequeña flecha ^). No: se queda en la barra de tareas como una ventana normal. En ambos casos, el contador sigue contando."),
        ["footerWindow"] = L.T("La croix range la fenêtre à côté de l'horloge : le compteur continue. Clic droit sur son icône → Fermer pour l'arrêter.", "The close button puts the window next to the clock: it keeps counting. Right click its icon → Close to stop it.", "La X manda la ventana junto al reloj: sigue contando. Clic derecho en su icono → Cerrar para detenerlo."),
        ["withFortnite"] = L.T("Ouvrir avec Fortnite", "Open with Fortnite", "Abrir con Fortnite"),
        ["withFortniteHelp"] = L.T("Un petit processus invisible attend Fortnite et ouvre le compteur, qui se ferme après.", "A tiny invisible process waits for Fortnite and opens the counter, which closes afterwards.", "Un pequeño proceso invisible espera a Fortnite y abre el contador, que se cierra después."),
        ["updates"] = L.T("Mises à jour", "Updates", "Actualizaciones"),
        ["upPrompt"] = L.T("Proposées", "Offered", "Propuestas"),
        ["upAuto"] = L.T("Installées automatiquement", "Installed automatically", "Instaladas automáticamente"),
        ["upOff"] = L.T("Désactivées", "Off", "Desactivadas"),
        ["profile"] = L.T("Compte (profil)", "Account (profile)", "Cuenta (perfil)"),
        ["demos"] = L.T("Dossier des replays", "Replays folder", "Carpeta de repeticiones"),
        ["demosMissing"] = L.T("Introuvable : Fortnite le crée à la première partie enregistrée.", "Not found: Fortnite creates it with the first recorded match.", "No encontrada: Fortnite la crea con la primera partida grabada."),
        ["save"] = L.T("Enregistrer", "Save", "Guardar"),
        ["editChests"] = L.T("Modifier mon nombre de coffres", "Edit my number of chests", "Modificar mi número de cofres"),
        ["close"] = L.T("Fermer", "Close", "Cerrar"),
        ["manual"] = L.T("Coffres ajoutés à la main", "Chests added by hand", "Cofres añadidos a mano"),
        ["manualHelp"] = L.T("Les coffres ouverts avant d'utiliser le compteur. Un nombre négatif en retire, 0 enlève l'ajout.", "The chests opened before using the counter. A negative number removes some, 0 clears it.", "Los cofres abiertos antes de usar el contador. Un número negativo quita cofres, 0 lo borra."),
        ["estimate"] = L.T("Ou estimer à partir de tes parties", "Or estimate from your matches", "O estimar a partir de tus partidas"),
        ["estimateHelp"] = L.T("Nombre de parties Battle Royale de la saison, dans Fortnite : Carrière → Statistiques.", "Number of Battle Royale matches this season, in Fortnite: Career → Stats.", "Número de partidas de Battle Royale de la temporada, en Fortnite: Carrera → Estadísticas."),
        ["career"] = L.T("Parties jouées", "Matches played", "Partidas jugadas"),
        ["compute"] = L.T("Calculer", "Compute", "Calcular"),
        ["add"] = L.T("Ajouter {0} coffres", "Add {0} chests", "Añadir {0} cofres"),
        ["estimateTooFew"] = L.T("Il faut au moins {0} parties comptées pour une moyenne fiable.", "At least {0} counted matches are needed for a reliable average.", "Hacen falta al menos {0} partidas contadas para una media fiable."),
        ["estimateNothing"] = L.T("Rien à ajouter : tes parties comptées couvrent déjà ce nombre.", "Nothing to add: your counted matches already cover that number.", "Nada que añadir: tus partidas contadas ya cubren ese número."),
        ["dataFolder"] = L.T("Ouvrir le dossier des données", "Open the data folder", "Abrir la carpeta de datos"),
        ["errorLog"] = L.T("Ouvrir le journal d'erreurs", "Open the error log", "Abrir el registro de errores"),
        ["issue"] = L.T("Signaler un problème (GitHub)", "Report a problem (GitHub)", "Informar de un problema (GitHub)"),
        ["project"] = L.T("Page du projet (GitHub)", "Project page (GitHub)", "Página del proyecto (GitHub)"),
        ["helpText"] = L.T("Le compteur lit les replays que Fortnite enregistre et compte tes coffres quelques secondes après chaque partie (Battle Royale de la saison seulement). Laisse-le ouvert pendant que tu joues : tu peux réduire la fenêtre, il continue de compter.",
            "The counter reads the replays Fortnite saves and counts your chests a few seconds after each match (the season's Battle Royale only). Keep it open while you play: you can minimize the window, it keeps counting.",
            "El contador lee las repeticiones que guarda Fortnite y cuenta tus cofres unos segundos después de cada partida (solo el Battle Royale de la temporada). Déjalo abierto mientras juegas: puedes minimizar la ventana, sigue contando."),
        ["uninstall"] = L.T("Désinstaller Pirate Shop Tracker", "Uninstall Pirate Shop Tracker", "Desinstalar Pirate Shop Tracker"),
        ["keepData"] = L.T("Garder mon historique et mes réglages", "Keep my history and settings", "Conservar mi historial y mis ajustes"),
        ["deleteData"] = L.T("Tout supprimer", "Delete everything", "Borrarlo todo"),
        ["uninstallConfirm"] = L.T("Désinstaller maintenant ? Le compteur se ferme et PirateShopTracker.exe est supprimé.", "Uninstall now? The counter closes and PirateShopTracker.exe is deleted.", "¿Desinstalar ahora? El contador se cierra y se borra PirateShopTracker.exe."),
        ["quit"] = L.T("Quitter le compteur", "Quit the counter", "Salir del contador"),
        ["quitConfirm"] = L.T("Arrêter le compteur ? Les parties jouées ensuite ne seront comptées qu'à sa prochaine ouverture.", "Stop the counter? Matches played afterwards are only counted when it is opened again.", "¿Detener el contador? Las partidas jugadas después solo se cuentan cuando se vuelva a abrir."),
        ["closed"] = L.T("Le compteur est arrêté. Tu peux fermer cette fenêtre.", "The counter is stopped. You can close this window.", "El contador está detenido. Puedes cerrar esta ventana."),
        ["offline"] = L.T("Le compteur ne répond plus (fermé ?). Relance PirateShopTracker.exe.", "The counter does not answer (closed?). Start PirateShopTracker.exe again.", "El contador no responde (¿cerrado?). Vuelve a abrir PirateShopTracker.exe."),
        ["footer"] = L.T("Fermer cette fenêtre ne coupe pas le compteur.", "Closing this window does not stop the counter.", "Cerrar esta ventana no detiene el contador."),
        ["pirateTitle"] = L.T("Accès à la boutique pirate", "Pirate Shop Access", "Acceso a la Tienda Pirata"),
        ["pirateSub"] = L.T("de chances de l'avoir déjà eu avec tes {0} coffres", "chance of having already got it with your {0} chests", "de probabilidad de haberlo conseguido ya con tus {0} cofres"),
        ["pirateOdds"] = L.T("1 sur {0} par coffre", "1 in {0} per chest", "1 entre {0} por cofre"),
        ["pirateNext"] = L.T("Prochain palier {0} · encore {1} coffres", "Next milestone {0} · {1} chests to go", "Próxima meta {0} · faltan {1} cofres"),
        ["pirateHalf"] = L.T("50 % à {0} coffres", "50% at {0} chests", "50 % a los {0} cofres"),
        ["pirateLive"] = L.T("{0} dans cette partie ({1} coffres)", "{0} this match ({1} chests)", "{0} en esta partida ({1} cofres)"),
        ["pirateScale"] = L.T("objectif : une chance sur deux", "goal: one chance in two", "meta: una de cada dos"),
        ["pirateWhy"] = L.T("Chaque coffre a toujours la même chance (1 sur {0}). Ce qui augmente à chaque coffre, c'est la chance de l'avoir obtenu au moins une fois : 1 − (1 − 1/{0})^coffres.",
            "Every chest always has the same odds (1 in {0}). What grows with every chest is the chance of having obtained it at least once: 1 − (1 − 1/{0})^chests.",
            "Cada cofre tiene siempre la misma probabilidad (1 entre {0}). Lo que sube con cada cofre es la probabilidad de haberlo conseguido al menos una vez: 1 − (1 − 1/{0})^cofres."),
        ["jet"] = L.T("HUD d'avion de chasse", "Fighter jet HUD", "HUD de caza (F-18)"),
        ["position"] = L.T("Position", "Position", "Posición"),
        ["positionHelp"] = L.T("Ancré, il reste à sa place (au centre : au-dessus du viseur, jamais dessus). Libre : déverrouille-le et déplace-le à la souris.", "Anchored, it stays in place (center: above the crosshair, never on it). Free: unlock it and drag it with the mouse.", "Anclado, se queda en su sitio (centro: encima de la mira, nunca sobre ella). Libre: desbloquéalo y arrástralo con el ratón."),
        ["aFree"] = L.T("Libre (à la souris)", "Free (drag it)", "Libre (arrastrarlo)"),
        ["aTl"] = L.T("Coin haut gauche", "Top left corner", "Esquina superior izquierda"),
        ["aTc"] = L.T("En haut au centre", "Top center", "Arriba al centro"),
        ["aTr"] = L.T("Coin haut droit", "Top right corner", "Esquina superior derecha"),
        ["aCenter"] = L.T("Centre (tête haute)", "Center (head-up)", "Centro (modo head-up)"),
        ["aBl"] = L.T("Coin bas gauche", "Bottom left corner", "Esquina inferior izquierda"),
        ["aBr"] = L.T("Coin bas droit", "Bottom right corner", "Esquina inferior derecha"),
        ["hudColor"] = L.T("Couleur du HUD", "HUD color", "Color del HUD"),
        ["hudColorHelp"] = L.T("Lignes, textes et jeton du HUD, et la couleur d'accent de cette fenêtre.", "Lines, text and token of the HUD, and the accent color of this window.", "Líneas, textos y token del HUD, y el color de acento de esta ventana."),
        ["anim"] = L.T("Animations du HUD", "HUD animations", "Animaciones del HUD"),
        ["animHelp"] = L.T("Scintillement du phosphore, balayage et petit glitch de temps en temps. Désactivées : image fixe, aucun calcul.", "Phosphor flicker, a scan sweep and a small glitch now and then. Off: a still picture, nothing computed.", "Parpadeo de fósforo, barrido de escaneo y un pequeño glitch de vez en cuando. Desactivadas: imagen fija, sin ningún cálculo."),
        ["screen"] = L.T("Écran", "Screen", "Pantalla"),
        ["chestsShort"] = L.T("coffres", "chests", "cofres"),
    };

    public static string Html(string token) => Template.Replace("__TOKEN__", token).Replace("__PENCIL__", ReportWriter.PencilIcon)
        .Replace("__LOGO__", Brand.Logo(34)).Replace("__LOGOSM__", Brand.Logo(18)).Replace("__FAVICON__", Brand.FaviconLink).Replace("__TOKENS__", ReportWriter.Tokens + ReportWriter.Military).Replace("__TOKENPIC__", "");

    private const string Template = """
        <!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
        <title>Pirate Shop Tracker</title>__FAVICON__
        <style>__TOKENS__
        *{box-sizing:border-box}
        html{background:var(--bg)}
        body{margin:0;background:var(--bg);color:var(--ink);font:14.5px/1.5 "Segoe UI Variable Text","Segoe UI",system-ui,sans-serif;-webkit-font-smoothing:antialiased}
        .num{font-family:Bahnschrift,"Segoe UI",sans-serif;font-variant-numeric:tabular-nums;letter-spacing:-.01em}
        .hidden{display:none!important}
        button{font:inherit}
        /* Layout: sidebar + content */
        .shell{display:grid;grid-template-columns:232px minmax(0,1fr);min-height:100vh}
        body.windowed .shell{min-height:calc(100vh - 36px)}
        #side{position:sticky;top:0;align-self:start;height:100vh;display:flex;flex-direction:column;gap:18px;padding:22px 14px 18px;border-right:1px solid var(--line);background:linear-gradient(180deg,#0a110c,#060a07)}
        body.windowed #side{height:calc(100vh - 36px)}
        .brand{display:flex;align-items:center;gap:11px;padding:0 8px}
        .brand b{display:block;font:700 13px/1.2 var(--mono);text-transform:uppercase;letter-spacing:.06em;color:var(--ink2)}
        .brand small{color:var(--sea);font:600 11px var(--mono);letter-spacing:.08em}
        .brand svg{flex:none}
        nav{display:flex;flex-direction:column;gap:2px}
        nav button{display:flex;align-items:center;gap:12px;min-height:40px;padding:8px 12px;border-radius:2px;border:0;border-left:2px solid transparent;background:transparent;color:var(--muted);font:600 12.5px var(--mono);text-transform:uppercase;letter-spacing:.08em;cursor:pointer;text-align:left}
        nav button svg{flex:none;opacity:.85}
        nav button:hover{background:var(--panel2);color:var(--ink)}
        nav button.on{background:var(--seabg);color:var(--sea);border-left-color:var(--sea)}
        nav button:focus-visible{outline:2px solid var(--sea);outline-offset:-2px}
        .sidefoot{margin-top:auto;display:flex;flex-direction:column;gap:10px;padding:0 4px}
        .who{display:flex;flex-direction:column;gap:6px;color:var(--muted);font-size:13px}
        .who .pname{color:var(--ink);font-weight:600}
        .who select{width:100%}
        #content{min-width:0;padding:26px 32px 40px;display:flex;flex-direction:column;gap:20px;max-width:1060px;width:100%}
        #app,#view{display:flex;flex-direction:column;gap:20px}
        #view:empty{display:none}
        .top{display:flex;align-items:center;justify-content:space-between;gap:12px;flex-wrap:wrap}
        .top h1{margin:0;font:700 22px/1.2 var(--mono);color:var(--ink2)}
        .pill{display:inline-flex;align-items:center;gap:8px;padding:6px 12px;border-radius:2px;background:var(--panel);border:1px solid var(--line2);color:var(--ink);font:600 12px var(--mono);text-transform:uppercase;letter-spacing:.06em}
        .pill i{width:8px;height:8px;border-radius:50%;background:var(--sea);box-shadow:0 0 0 3px var(--seabg)}
        .pill.live i{background:var(--live);box-shadow:0 0 0 3px var(--livebg);animation:pulse 1.6s ease-in-out infinite}
        .pill.busy i{background:var(--gold);box-shadow:0 0 0 3px var(--goldbg)}.pill.warn i{background:var(--warn);box-shadow:0 0 0 3px var(--warnbg)}
        @keyframes pulse{50%{opacity:.35}}
        @media (prefers-reduced-motion:reduce){.pill.live i{animation:none}}
        h2{margin:0 0 12px;font:700 14px/1.3 var(--mono);color:var(--ink2)}
        #rep{width:100%;min-height:400px;border:0;display:block;background:transparent;overflow:hidden}
        /* Cards */
        .card{background:var(--panel);color:var(--ink);border:1px solid var(--line);border-radius:var(--radius);padding:18px 20px}
        .card.flat{padding:4px 20px}
        /* Pirate Shop hero */
        .hero{display:grid;grid-template-columns:minmax(0,1.7fr) minmax(0,1fr);gap:14px}
        .phero{position:relative;background:radial-gradient(120% 140% at 0% 0%,var(--seabg),transparent 55%),var(--panel);border:1px solid var(--seaglow);border-radius:var(--radius);padding:22px 24px 20px}
        .phero .tokenpic{position:absolute;right:20px;top:20px;width:150px;height:150px;filter:drop-shadow(0 0 22px var(--seaglow));pointer-events:none}
        .phero>.eyebrow,.phero>.pct,.phero>.psub{max-width:calc(100% - 150px)}
        @media (max-width:720px){.phero .tokenpic{width:96px;height:96px}.phero>.eyebrow,.phero>.pct,.phero>.psub{max-width:calc(100% - 96px)}}
        .eyebrow{display:flex;align-items:center;gap:8px;font:700 12px var(--mono);letter-spacing:.12em;text-transform:uppercase;color:var(--sea)}
        .pct{font:700 64px/1.05 var(--mono);color:var(--sea);margin:10px 0 2px;text-shadow:0 0 28px var(--seaglow);letter-spacing:-.02em}
        .psub{color:var(--muted);font-size:14px}.psub b{color:var(--ink)}
        .pmeter{position:relative;height:10px;border-radius:1px;background:repeating-linear-gradient(90deg,var(--line2) 0 1px,transparent 1px 10%),var(--track);margin:18px 0 6px;overflow:hidden}
        .pmeter i{position:absolute;left:0;top:0;bottom:0;border-radius:1px;background:linear-gradient(90deg,var(--sea2),var(--sea));box-shadow:0 0 10px var(--seaglow);min-width:6px;transition:width .6s ease}
        .pscale{display:flex;justify-content:space-between;font:600 11px var(--mono);text-transform:uppercase;letter-spacing:.06em;color:var(--muted)}
        .chips{display:flex;flex-wrap:wrap;gap:8px;margin-top:14px}
        .chip{display:inline-flex;align-items:center;gap:6px;padding:5px 10px;border-radius:2px;background:var(--panel2);border:1px solid var(--line2);font:600 12px var(--mono);color:var(--ink)}
        .chip.live{background:var(--livebg);border-color:rgba(255,77,94,.4);color:#ff8a96}
        .chip.live i{width:7px;height:7px;border-radius:50%;background:var(--live)}
        .info{position:relative;display:inline-grid;place-items:center;width:18px;height:18px;border-radius:50%;border:1.5px solid var(--line2);color:var(--muted);font:700 11px/1 "Segoe UI",sans-serif;cursor:help;outline:none;letter-spacing:0;text-transform:none}
        .info:hover,.info:focus{border-color:var(--sea);color:var(--sea)}
        .info .tip{display:none;position:absolute;z-index:5;top:24px;left:-10px;width:320px;max-width:70vw;background:#132017;color:var(--ink);border:1px solid var(--line2);padding:10px 12px;border-radius:3px;text-transform:none;letter-spacing:0;font:400 13px/1.5 "Segoe UI",system-ui,sans-serif;box-shadow:0 12px 32px rgba(0,0,0,.5)}
        .info:hover .tip,.info:focus .tip{display:block}
        .chests{position:relative;display:flex;flex-direction:column;justify-content:space-between;gap:10px;background:linear-gradient(160deg,var(--goldbg),transparent 60%),var(--panel);border:1px solid rgba(255,196,64,.3);border-radius:var(--radius);padding:22px 22px 20px}
        .chests .k{font:700 12px var(--mono);letter-spacing:.12em;text-transform:uppercase;color:var(--gold)}
        .chests .v{font:700 48px/1.05 var(--mono);color:var(--ink2);letter-spacing:-.02em}
        .chests .l{font-size:13px;color:var(--muted)}
        .editbtn{position:absolute;top:14px;right:14px;width:34px;height:34px;padding:0;border-radius:2px;border:1px solid var(--line2);background:var(--panel2);color:var(--muted);cursor:pointer;display:grid;place-items:center}
        .editbtn:hover{color:var(--gold);border-color:var(--gold)}.editbtn:focus-visible{outline:2px solid var(--gold);outline-offset:2px}
        .tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(170px,1fr));gap:12px}
        .tile{background:var(--panel);border:1px solid var(--line);border-radius:var(--radius);padding:14px 16px}
        .tile .k{font:600 11px var(--mono);text-transform:uppercase;letter-spacing:.08em;color:var(--muted)}
        .tile .v{font:700 28px/1.2 var(--mono);color:var(--ink2);margin-top:2px}
        .tile .l{font-size:12.5px;color:var(--muted)}
        .tile .v.star{color:var(--star)}
        /* Rows, buttons, inputs */
        .row{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:14px 0;border-bottom:1px solid var(--line)}
        .row:last-child{border-bottom:none}
        .row .t{font-weight:600;color:var(--ink)}.row .h{font-size:13px;color:var(--muted);margin-top:2px;max-width:620px}
        .actions{display:flex;flex-wrap:wrap;gap:8px}
        button.b{min-height:38px;padding:8px 16px;border-radius:2px;font-family:var(--mono)!important;text-transform:uppercase;letter-spacing:.06em;font-size:12px!important;border:1px solid var(--line2);background:var(--panel2);color:var(--ink);font-weight:600;font-size:13.5px;cursor:pointer;transition:border-color .12s,color .12s,background .12s}
        button.b:hover{border-color:var(--sea);color:var(--sea)}
        button.b:focus-visible{outline:2px solid var(--sea);outline-offset:2px}
        button.b.main{background:var(--sea);border-color:var(--sea);color:#04140a}
        button.b.main:hover{filter:brightness(1.12);color:#04140a}
        button.b.danger{border-color:rgba(255,77,94,.45);color:#ff8a96;background:transparent}
        button.b.danger:hover{background:var(--livebg)}
        select,input[type=text],input[type=number]{min-height:38px;padding:6px 10px;border-radius:2px;border:1px solid var(--line2);background:var(--bg);color:var(--ink);font:14px "Segoe UI",system-ui,sans-serif}
        select:focus,input:focus{outline:2px solid var(--sea);outline-offset:1px;border-color:transparent}
        select option{background:#0b120d;color:var(--ink)}
        input[type=text]{width:min(520px,100%)}
        .switch{position:relative;display:inline-block;width:44px;height:24px;flex:none;vertical-align:middle}
        .switch input{opacity:0;width:100%;height:100%;margin:0;cursor:pointer;position:absolute;z-index:1}
        .switch span{position:absolute;inset:0;border-radius:2px;background:var(--line2);transition:background .15s}
        .switch span:after{content:"";position:absolute;top:3px;left:3px;width:18px;height:18px;border-radius:1px;background:#fff;transition:transform .15s}
        .switch input:checked+span{background:var(--sea2);box-shadow:0 0 10px var(--seaglow)}.switch input:checked+span:after{transform:translateX(20px)}
        .switch input:focus-visible+span{outline:2px solid var(--sea);outline-offset:2px}
        .swatches{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
        .swatch{width:28px;height:28px;border-radius:2px;border:2px solid var(--line2);background:var(--c);cursor:pointer;box-shadow:inset 0 0 0 3px var(--panel)}
        .swatch.on{border-color:var(--c);box-shadow:inset 0 0 0 3px var(--panel),0 0 12px var(--c)}
        .swatch:focus-visible{outline:2px solid var(--ink);outline-offset:2px}
        .swatches input[type=color]{width:36px;height:30px;padding:0 2px;border:1px solid var(--line2);border-radius:2px;background:var(--bg);cursor:pointer}
        .badge{display:inline-block;margin-left:6px;padding:1px 8px;border-radius:999px;background:var(--warnbg);color:var(--warn);font-size:11px;font-weight:700;vertical-align:1px;cursor:help}
        .log{font:12.5px/1.6 var(--mono);max-height:280px;overflow:auto;white-space:pre-wrap;color:var(--muted)}
        .log .err{color:#ff8a96}.log .warn{color:var(--gold)}.log .ok{color:var(--sea)}.log .info{color:var(--team)}.log .tre{color:var(--star)}.log .mut{color:#5d6a84}
        .bar{height:6px;background:var(--track);border-radius:3px;overflow:hidden;margin-top:10px}.bar i{display:block;height:100%;background:var(--sea);border-radius:3px}
        .path{font:12.5px "Cascadia Mono",Consolas,monospace;word-break:break-all;background:var(--bg);border:1px solid var(--line);padding:8px 10px;border-radius:9px;display:block;margin:6px 0 10px;color:var(--ink)}
        .note{font-size:13px;color:var(--muted)}
        footer{display:flex;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap;border-top:1px solid var(--line);padding-top:14px;color:var(--muted);font-size:12.5px}
        .estimate{margin-top:8px;font-weight:600}
        .notice{position:relative;display:flex;gap:12px;align-items:flex-start;border-left:4px solid var(--gold)}
        .notice.warning{border-left-color:var(--live);background:var(--livebg)}
        .notice .ni{flex:none;width:22px;height:22px;border-radius:50%;display:grid;place-items:center;font:700 13px "Segoe UI",sans-serif;background:var(--gold);color:#1a1200;margin-top:1px}
        .notice.warning .ni{background:var(--live);color:#fff}
        .notice .nb{flex:1;min-width:0}.notice .nt{font-weight:700;margin-bottom:2px}.notice .nx{white-space:pre-line}
        .notice .actions{margin-top:10px}
        .notice .nclose{flex:none;width:30px;height:30px;margin:-6px -8px 0 0;border:0;border-radius:8px;background:transparent;color:var(--muted);font-size:20px;line-height:1;cursor:pointer}
        .notice .nclose:hover{background:var(--panel2);color:var(--ink)}
        .welcome{border-color:rgba(52,230,196,.35)}
        .welcome h2{font-size:20px;color:var(--sea)}
        dialog{width:min(560px,calc(100% - 32px));padding:20px 22px;border:1px solid var(--seaglow);border-radius:3px;background:var(--panel);color:var(--ink);box-shadow:0 24px 64px rgba(0,0,0,.6)}
        dialog::backdrop{background:rgba(3,6,12,.7)}
        .dhead{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:6px}
        .dhead h2{margin:0;font-size:18px}
        .dclose{width:36px;height:36px;border-radius:2px;border:1px solid var(--line2);background:transparent;color:var(--ink);font-size:20px;line-height:1;cursor:pointer}
        .dclose:hover{border-color:var(--sea);color:var(--sea)}
        @media (max-width:860px){.hero{grid-template-columns:1fr}}
        @media (max-width:720px){
          .shell{grid-template-columns:1fr}
          #side{position:static;height:auto!important;flex-direction:row;flex-wrap:wrap;align-items:center;padding:12px 16px;border-right:0;border-bottom:1px solid var(--line)}
          nav{flex-direction:row;flex-wrap:wrap}nav button span{display:none}
          .sidefoot{display:none}
          #content{padding:20px 16px 32px}
          .row{flex-direction:column;align-items:flex-start}
          .pct{font-size:48px}
        }
        /* The counter's own window: its title bar is drawn here (drag to move, double click to maximize). */
        .tbar{display:none}
        body.windowed{height:100vh;overflow:hidden}
        body.windowed .tbar{display:flex;align-items:center;justify-content:space-between;position:fixed;top:0;left:0;right:0;height:36px;background:#060a07;border-bottom:1px solid var(--line);z-index:20;app-region:drag;user-select:none}
        .tbar .tt img{width:18px;height:18px}
        .tbar .tt{display:flex;align-items:center;gap:8px;padding-left:12px;font:600 11px var(--mono);text-transform:uppercase;letter-spacing:.1em;color:var(--muted)}
        body.windowed #scroll{position:absolute;top:36px;left:0;right:0;bottom:0;overflow:auto}
        body.windowed #side select,body.windowed #side button{app-region:no-drag}
        .wctl{display:flex;app-region:no-drag}
        .wctl button{width:46px;height:36px;padding:0;border:0;border-radius:0;background:transparent;color:var(--muted);display:grid;place-items:center;cursor:pointer;transition:background .12s,color .12s}
        .wctl button:hover{background:var(--panel2);color:var(--ink)}
        .wctl button.x:hover{background:#c42b3a;color:#fff}
        .wctl button:focus-visible{outline:2px solid var(--sea);outline-offset:-2px}
        .wctl svg{display:block}
        #scroll::-webkit-scrollbar,html::-webkit-scrollbar{width:12px}
        #scroll::-webkit-scrollbar-track,html::-webkit-scrollbar-track{background:var(--bg)}
        #scroll::-webkit-scrollbar-thumb,html::-webkit-scrollbar-thumb{background:var(--line2);border-radius:6px;border:3px solid var(--bg)}
        #scroll::-webkit-scrollbar-thumb:hover,html::-webkit-scrollbar-thumb:hover{background:#3a4d73}
        .log::-webkit-scrollbar{width:10px}
        .log::-webkit-scrollbar-track{background:transparent}
        .log::-webkit-scrollbar-thumb{background:var(--line2);border-radius:5px;border:2px solid var(--panel)}
        </style></head><body><div class="tbar"><div class="tt">__LOGOSM__<span>Pirate Shop Tracker</span></div><div class="wctl">
          <button type="button" id="wmin"><svg width="10" height="10" viewBox="0 0 10 10" aria-hidden="true"><path d="M0 5.5h10" stroke="currentColor" stroke-width="1.1"/></svg></button>
          <button type="button" id="wmax"><svg width="10" height="10" viewBox="0 0 10 10" aria-hidden="true"><rect id="wmaxicon" x=".55" y=".55" width="8.9" height="8.9" fill="none" stroke="currentColor" stroke-width="1.1"/><path id="wresicon" d="M2.5 2.5V.55h6.95v6.95H7.5M.55 2.5h6.95v6.95H.55z" fill="none" stroke="currentColor" stroke-width="1.1" style="display:none"/></svg></button>
          <button type="button" id="wclose" class="x"><svg width="10" height="10" viewBox="0 0 10 10" aria-hidden="true"><path d="M.5.5l9 9M9.5.5l-9 9" stroke="currentColor" stroke-width="1.2"/></svg></button>
        </div></div><div id="scroll"><div class="shell"><aside id="side"></aside><div id="content"><div id="app"><p class="note">…</p></div><section id="reportPane" class="hidden"><iframe id="rep" title="Report" scrolling="no"></iframe></section><div id="foot"></div></div></div></div>
        <dialog id="dlg" aria-labelledby="dlgT"><div class="dhead"><h2 id="dlgT"></h2><button type="button" class="dclose" id="dlgX">×</button></div><div id="dlgB"></div></dialog>
        <script>
        const TOKEN='__TOKEN__';const LOGO=`__LOGO__`;let TOKENPIC='',tokenColor=null;let S=null,tab=(()=>{const h=location.hash.slice(1);if(['home','report','overlay','settings','help'].includes(h))return h;try{return localStorage.getItem('cc-gui-tab')||'home'}catch(e){return 'home'}})(),estimate=null,stopped=false,copied=false;
        const $=s=>document.querySelector(s);
        const ICONS={
          home:'<path d="M3 10.5 12 4l9 6.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z"/>',
          report:'<path d="M4 20V11M10 20V5M16 20v-6M21 20H3"/>',
          overlay:'<rect x="3" y="5" width="18" height="14" rx="2.5"/><path d="M7 9.5h6M7 13h3"/>',
          settings:'<path d="M4 7h9M17 7h3M4 12h3M11 12h9M4 17h11M19 17h1"/><circle cx="15" cy="7" r="2"/><circle cx="9" cy="12" r="2"/><circle cx="17" cy="17" r="2"/>',
          help:'<circle cx="12" cy="12" r="9"/><path d="M9.6 9.3a2.5 2.5 0 1 1 3.4 2.3c-.6.3-1 .8-1 1.5v.4M12 16.8h.01"/>'};
        const icon=k=>`<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${ICONS[k]}</svg>`;
        // In the counter's own window (?w=1): the title bar of the page, its buttons sent to the window.
        const host=window.chrome&&window.chrome.webview;let maximized=false;
        if(host&&new URLSearchParams(location.search).get('w')==='1')document.body.classList.add('windowed');
        const scroller=()=>document.body.classList.contains('windowed')?document.getElementById('scroll'):document.scrollingElement;
        function winBtn(a){if(host)host.postMessage({win:a})}
        function winTitles(){if(!S)return;const T=S.txt;$('#wmin').title=T.wMin;$('#wmin').setAttribute('aria-label',T.wMin);const m=maximized?T.wRestore:T.wMax;$('#wmax').title=m;$('#wmax').setAttribute('aria-label',m);$('#wclose').title=T.wClose;$('#wclose').setAttribute('aria-label',T.wClose)}
        if(host)host.addEventListener('message',e=>{const d=e.data;if(d&&typeof d.maximized==='boolean'){maximized=d.maximized;$('#wmaxicon').style.display=maximized?'none':'';$('#wresicon').style.display=maximized?'':'none';winTitles()}});
        $('.tbar').addEventListener('dblclick',e=>{if(!e.target.closest('.wctl'))winBtn('max')});
        const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
        const fmt=(s,...a)=>a.reduce((r,v,i)=>r.split('{'+i+'}').join(v),s);
        async function api(action,value){const r=await fetch('/api/action',{method:'POST',headers:{'X-Token':TOKEN,'Content-Type':'application/json'},body:JSON.stringify({action,value})});const j=await r.json();await refresh(true);return j.result}
        async function refresh(force){if(stopped)return;try{const r=await fetch('/api/state',{headers:{'X-Token':TOKEN}});S=await r.json();render(force)}catch(e){if(S){$('#app').innerHTML='<div class="card">'+esc(S.txt.offline)+'</div>'}}}
        function busy(){const a=document.activeElement;return a&&a.closest&&a.closest('#view')&&/INPUT|SELECT|TEXTAREA/.test(a.tagName)}
        function busySide(){const a=document.activeElement;return a&&a.closest&&a.closest('#side')&&a.tagName==='SELECT'}
        function sw(id,on,act){return `<label class="switch"><input type="checkbox" id="${id}" ${on?'checked':''} onchange="api('${act}',this.checked)"><span></span></label>`}
        function row(title,help,control){return `<div class="row"><div><div class="t">${title}</div>${help?`<div class="h">${help}</div>`:''}</div><div>${control}</div></div>`}
        function sel(act,value,opts){return `<select onchange="api('${act}',this.value)">${opts.map(o=>`<option value="${esc(o[0])}" ${String(o[0])===String(value)?'selected':''}>${esc(o[1])}</option>`).join('')}</select>`}
        function setTab(t){tab=t;try{localStorage.setItem('cc-gui-tab',t)}catch(e){}render(true);scroller().scrollTop=0}
        function side(T){
          const who=S.profiles.length>1?`<select aria-label="${esc(T.profile)}" title="${esc(T.profile)}" onchange="api('profile',this.value)">${S.profiles.map(p=>`<option value="${esc(p.key)}" ${p.key===S.profileKey?'selected':''}>${esc(p.name)} (${esc(p.matches)})</option>`).join('')}</select>`:(S.player?`<span class="pname">${esc(S.player)}</span>`:'');
          return `<div class="brand">${LOGO}<div><b>${esc(S.appName)}</b><small>v${esc(S.version)}</small></div></div>
            <nav>${['home','report','overlay','settings','help'].map(t=>`<button class="${t===tab?'on':''}" onclick="setTab('${t}')" ${t===tab?'aria-current="page"':''}>${icon(t)}<span>${esc(T[t])}</span></button>`).join('')}</nav>
            <div class="sidefoot"><div class="who">${who}</div></div>`;
        }
        function topBar(T){return `<div class="top" id="top"><h1>${esc(T[tab])}</h1><span class="pill ${S.status.kind}" role="status"><i></i>${esc(S.status.text)}</span></div>`}
        function render(force){
          const T=S.txt;document.documentElement.lang=S.lang;document.documentElement.style.setProperty('--sea',S.overlay.color);
          if(force||!busySide())$('#side').innerHTML=side(T);
          if(!force&&busy()){const t=$('#top');if(t)t.outerHTML=topBar(T);return}
          let v='';
          if(tab==='home')v=home(T);else if(tab==='overlay')v=overlay(T);else if(tab==='settings')v=settings(T);else if(tab==='help')v=help(T);
          $('#app').innerHTML=`${topBar(T)}${notices(T)}<div id="view">${v}</div>`;
          // In the counter's own window the close button asks and stops it: no "quit" button needed.
          $('#foot').innerHTML=S.settings.windowed?`<footer><span>${esc(T.footerWindow)}</span></footer>`:`<footer><span>${esc(T.footer)}</span><button class="b danger" onclick="quit()">${esc(T.quit)}</button></footer>`;
          showReport();winTitles();
        }
        // The report, in the window: loaded once, then again only when the counter rewrote it (new match, setting...).
        let repStamp=null,repObs=null;
        function fitReport(){const f=$('#rep');try{const m=f.contentDocument.querySelector('main');f.style.height=Math.ceil(m.getBoundingClientRect().height)+'px'}catch(e){}}
        function showReport(){
          const pane=$('#reportPane'),f=$('#rep');pane.classList.toggle('hidden',tab!=='report');
          if(tab!=='report'||S.reportStamp===repStamp)return;
          const y=scroller().scrollTop;repStamp=S.reportStamp;
          f.onload=()=>{fitReport();scroller().scrollTop=y;try{if(repObs)repObs.disconnect();repObs=new ResizeObserver(fitReport);repObs.observe(f.contentDocument.querySelector('main'))}catch(e){}};
          f.src='/report?t='+TOKEN+'&v='+encodeURIComponent(repStamp);
        }
        function notices(T){return (S.notices||[]).map(n=>`<div class="card notice ${n.level==='warning'?'warning':''}" role="${n.level==='warning'?'alert':'status'}"><span class="ni" aria-hidden="true">${n.level==='warning'?'!':'i'}</span><div class="nb">${n.title?`<div class="nt">${esc(n.title)}</div>`:''}<div class="nx">${esc(n.text)}</div>${n.link?`<div class="actions"><button class="b ${n.level==='warning'?'main':''}" onclick="window.open('${esc(n.link)}','_blank')">${esc(T.moreInfo)}</button></div>`:''}</div>${n.level==='info'?`<button class="nclose" title="${esc(T.close)}" aria-label="${esc(T.close)}" onclick="api('dismissNotice','${esc(n.id)}')">×</button>`:''}</div>`).join('')}
        // The token of the home card, tinted like the HUD (asked again only when the color changes).
        function tokenPic(){if(tokenColor!==S.overlay.color){tokenColor=S.overlay.color;fetch('/api/action',{method:'POST',headers:{'X-Token':TOKEN,'Content-Type':'application/json'},body:JSON.stringify({action:'tokenImage',value:150})}).then(r=>r.json()).then(j=>{TOKENPIC=(j.result||'').replace('<img ','<img class="tokenpic" ');const p=$('.phero');if(p&&!p.querySelector('.tokenpic'))p.insertAdjacentHTML('afterbegin',TOKENPIC);else if(p)p.querySelector('.tokenpic').outerHTML=TOKENPIC}).catch(()=>{})}return TOKENPIC}
        function pirateCard(T){
          const p=S.pirate,w=Math.max(0.8,Math.min(100,p.chance/0.5*100)).toFixed(2);
          return `<div class="phero ret">${tokenPic()}
            <div class="eyebrow">${esc(T.pirateTitle)}<span class="info" tabindex="0" role="note" aria-label="${esc(fmt(T.pirateWhy,p.oneIn))}">i<span class="tip">${esc(fmt(T.pirateWhy,p.oneIn))}</span></span></div>
            <div class="pct num">${esc(p.percent)}</div>
            <div class="psub">${fmt(esc(T.pirateSub),'<b class="num">'+esc(p.chests)+'</b>')}</div>
            <div class="pmeter" role="img" aria-label="${esc(p.percent)} / 50 %"><i style="width:${w}%"></i></div>
            <div class="pscale"><span>0 %</span><span>${esc(T.pirateScale)} · 50 %</span></div>
            <div class="chips"><span class="chip">${esc(fmt(T.pirateOdds,p.oneIn))}</span><span class="chip">${esc(fmt(T.pirateNext,p.next,p.toNext))}</span><span class="chip">${esc(fmt(T.pirateHalf,p.half))}</span>${p.live?`<span class="chip live"><i></i>${esc(fmt(T.pirateLive,p.live,p.liveChests))}</span>`:''}</div></div>`;
        }
        function home(T){
          let h='';
          if(S.autoClose)h+=`<div class="card notice warning"><div class="nb"><b>${esc(S.status.text)}</b><br>${esc(T.autoCloseText)}<div class="actions"><button class="b main" onclick="api('keepOpen')">${esc(T.keepOpen)}</button></div></div></div>`;
          if(S.firstRun){const s=S.settings;h+=`<div class="card welcome"><h2>${esc(T.welcome)}</h2><p style="margin-top:0;color:var(--muted)">${esc(T.welcomeText)}</p>
            <div class="t" style="font-weight:600;margin-top:14px">${esc(T.welcomeQuick)}</div>
            ${row(esc(T.language),'',sel('language',s.language,s.languages))}
            ${row(esc(T.inGame),esc(T.inGameHelp),sw('wov',S.overlay.show,'showOverlay'))}
            ${s.windowed?row(esc(T.tray),esc(T.trayHelp),sw('wtr',s.minimizeToTray,'minimizeToTray')):''}
            <div class="actions" style="margin-top:12px"><button class="b main" onclick="api('dismissWelcome')">${esc(T.welcomeDone)}</button></div></div>`}
          if(S.progress)h+=`<div class="card"><b>${esc(T.analyzing)} ${S.progress.done} / ${S.progress.total}</b><div class="bar"><i style="width:${Math.round(100*S.progress.done/Math.max(1,S.progress.total))}%"></i></div></div>`;
          if(S.whatsNew&&S.whatsNew.length)h+=`<div class="card"><h2>${esc(fmt(T.whatsNew,S.version))}</h2><ul>${S.whatsNew.map(n=>`<li>${esc(n)}</li>`).join('')}</ul><button class="b main" onclick="api('dismissWhatsNew')">${esc(T.ok)}</button></div>`;
          if(S.update)h+=`<div class="card"><h2>${esc(fmt(T.newVersion,S.update.version))}</h2><ul>${S.update.notes.map(n=>`<li>${esc(n)}</li>`).join('')}</ul><button class="b main" onclick="api('installUpdate')">${esc(T.install)}</button></div>`;
          h+=`<section class="hero">${pirateCard(T)}
            <div class="chests ret"><button type="button" class="editbtn" onclick="openManual()" title="${esc(T.editChests)}" aria-label="${esc(T.editChests)}">__PENCIL__</button>
              <div class="k">${esc(T.total)}</div><div><div class="v num">${esc(S.total)}</div><div class="l">${esc(T.totalSub)}${S.manual?'<br>'+esc(fmt(T.byHand,S.manualText)):''}</div></div>
              <div class="l">${esc(S.matches)} ${esc(T.matches)} · ${esc(fmt(T.perMatch,S.average))}</div></div></section>`;
          h+=`<section class="tiles">
            <div class="tile ret"><div class="k">${esc(T.session)}</div><div class="v num">${esc(S.session)}</div><div class="l">${esc(S.sessionMatches)} ${esc(T.matches)}</div></div>
            <div class="tile ret"><div class="k">${esc(T.last)}</div><div class="v num">${esc(S.last?S.last.chests:'—')}</div><div class="l">${S.last?esc(S.last.when)+' · '+esc(S.last.mode):''}</div></div>
            ${S.squad?`<div class="tile ret"><div class="k">${esc(T.squad)}<span class="badge" title="${esc(T.inaccurateWhy)}">${esc(T.inaccurate)}</span></div><div class="v num">${esc(S.squad.total)}</div><div class="l">${esc(fmt(T.squadSub,S.squad.team))}</div></div>`:''}
            <div class="tile ret"><div class="k">${esc(T.treasures)}</div><div class="v num star">★ ${esc(S.treasures)}</div><div class="l">${esc(S.matches)} ${esc(T.matches)}</div></div></section>`;
          h+=`<div class="actions"><button class="b main" onclick="setTab('report')">${esc(T.openReport)}</button>`;
          if(S.overlay.running)h+=`<button class="b" onclick="api('toggleOverlayVisible')">${esc(S.overlay.visible?T.hideOverlay:T.showOverlay)}</button><button class="b" onclick="api('toggleOverlayLock')">${esc(S.overlay.locked?T.unlock:T.lock)}</button>`;
          if(!S.update&&S.updatesOn)h+=`<button class="b" onclick="api('checkUpdate')">${esc(T.checkUpdate)}</button>`;
          h+=`</div><section><h2>${esc(T.activity)}</h2><div class="card log">${S.activity.length?S.activity.map(l=>l.map(s=>`<span class="${s.c}">${esc(s.t)}</span>`).join('')).join('\n'):esc(T.noActivity)}</div></section>`;
          return h;
        }
        function overlay(T){
          const o=S.overlay;
          return `<div class="card flat">${row(esc(T.inGame),esc(T.inGameHelp),sw('ov',o.show,'showOverlay'))}
            ${row(esc(T.style),'',sel('overlayStyle',o.style,[['jet',T.jet],['hud',T.hud],['classic',T.classic]]))}
            ${row(esc(T.position),esc(T.positionHelp),sel('overlayAnchor',o.anchor,[['free',T.aFree],['tl',T.aTl],['tc',T.aTc],['tr',T.aTr],['center',T.aCenter],['bl',T.aBl],['br',T.aBr]]))}
            ${o.monitors.length>1&&o.anchor!=='free'?row(esc(T.screen),'',sel('overlayMonitor',o.monitor,o.monitors)):''}
            ${row(esc(T.hudColor),esc(T.hudColorHelp),colors(o.color))}
            ${o.style==='jet'?row(esc(T.anim),esc(T.animHelp),sw('oan',o.animations,'overlayAnimations')):''}
            ${o.show?row(esc(T.size),'',sel('overlaySize',o.size,o.sizes.map(x=>[x,x+' %'])))+row(esc(T.opacity),'',sel('overlayOpacity',o.opacity,o.opacities.map(x=>[x,x+' %']))):''}
            ${row(esc(T.squadToggle)+`<span class="badge" title="${esc(T.inaccurateWhy)}">${esc(T.inaccurate)}</span>`,esc(T.squadHelp)+'<br>'+esc(T.inaccurateWhy),sw('sq',o.squad,'showSquadTotal'))}</div>
            <section><h2>${esc(T.obs)}</h2><div class="card"><span class="path">${esc(S.paths.overlay)}</span><p class="note">${esc(T.obsSteps)}</p>
            <div class="actions"><button class="b main" onclick="copyObs()">${esc(copied?T.copied:T.copy)}</button><button class="b" onclick="window.open('/overlay?t='+TOKEN,'_blank')">${esc(T.preview)}</button></div></div></section>`;
        }
        const HUDCOLORS=[['#5AFF96','Verde militar'],['#B6FF3B','Verde lima'],['#FFC440','Ámbar'],['#4CE0FF','Cian'],['#FF5A5A','Rojo'],['#F0F4FA','Blanco']];
        function colors(cur){return `<div class="swatches">${HUDCOLORS.map(c=>`<button type="button" class="swatch ${c[0]===cur?'on':''}" style="--c:${c[0]}" title="${esc(c[1])}" aria-label="${esc(c[1])}" onclick="api('overlayColor','${c[0]}')"></button>`).join('')}<input type="color" value="${esc(cur.toLowerCase())}" aria-label="Color" onchange="api('overlayColor',this.value)"></div>`}
        function settings(T){
          const s=S.settings;
          return `<div class="card flat">${row(esc(T.language),'',sel('language',s.language,s.languages))}
            ${S.profiles.length>1?row(esc(T.profile),'',sel('profile',S.profileKey,S.profiles.map(p=>[p.key,p.name+' ('+p.matches+' · '+p.chests+')']))):''}
            ${s.windowed?row(esc(T.tray),esc(T.trayHelp),sw('tr',s.minimizeToTray,'minimizeToTray')):''}
            ${s.canStartWithFortnite?row(esc(T.withFortnite),esc(T.withFortniteHelp),sw('wf',s.startWithFortnite,'startWithFortnite')):''}
            ${s.canUpdate?row(esc(T.updates),'',sel('updateMode',s.updateMode,[['prompt',T.upPrompt],['auto',T.upAuto],['off',T.upOff]])):''}
            <div class="row"><div style="flex:1"><div class="t">${esc(T.demos)}</div>${s.demosExists?'':`<div class="h">${esc(T.demosMissing)}</div>`}<div class="actions" style="margin-top:8px"><input type="text" id="demos" value="${esc(s.demos)}"><button class="b" onclick="api('demosFolder',$('#demos').value)">${esc(T.save)}</button></div></div></div></div>`;
        }
        // Chests added by hand (and the estimate from the Career matches): p = 's' in the settings, 'd' in the edit box.
        function manualForm(T,p){
          return `<p class="note" style="margin-top:0">${esc(T.manualHelp)}</p>
            <div class="actions"><input type="number" id="${p}manual" value="${S.manual}" step="1" aria-label="${esc(T.manual)}"><button class="b main" onclick="saveManual('${p}',parseInt($('#${p}manual').value||'0',10))">${esc(T.save)}</button></div>
            <div class="t" style="margin-top:18px;font-weight:600">${esc(T.estimate)}</div><p class="note">${esc(T.estimateHelp)}</p>
            <div class="actions"><input type="number" id="${p}career" min="0" placeholder="${esc(T.career)}" aria-label="${esc(T.career)}"><button class="b" onclick="doEstimate('${p}')">${esc(T.compute)}</button></div>
            <div class="estimate" id="${p}est">${estimate&&estimate.p===p?estimateHtml(T,p):''}</div>`;
        }
        function estimateHtml(T,p){if(estimate.error)return esc(estimate.error);return esc(estimate.text)+`<div class="actions" style="margin-top:8px"><button class="b main" onclick="saveManual('${p}',${estimate.chests})">${esc(fmt(T.add,estimate.chestsText))}</button></div>`}
        async function doEstimate(p){const r=await api('estimate',parseInt($('#'+p+'career').value||'0',10));estimate=Object.assign({p},r);const e=$('#'+p+'est');if(e)e.innerHTML=estimateHtml(S.txt,p)}
        async function saveManual(p,n){estimate=null;await api('manualChests',n);if(p==='d')$('#dlg').close();else render(true)}
        function openManual(){const T=S.txt,d=$('#dlg');estimate=null;$('#dlgT').textContent=T.editChests;$('#dlgB').innerHTML=manualForm(T,'d');$('#dlgX').setAttribute('aria-label',T.close);if(!d.open)d.showModal();const i=$('#dmanual');if(i){i.focus();i.select()}}
        $('#dlgX').onclick=()=>$('#dlg').close();
        $('#wmin').onclick=()=>winBtn('min');$('#wmax').onclick=()=>winBtn('max');$('#wclose').onclick=()=>winBtn('close');
        $('#dlg').addEventListener('click',e=>{if(e.target.id==='dlg')$('#dlg').close()});
        window.addEventListener('message',e=>{if(e.origin===location.origin&&e.data&&e.data.cc==='editChests'&&S)openManual()});
        async function copyObs(){await api('copyObsPath');copied=true;render(true);setTimeout(()=>{copied=false;render(true)},2000)}
        function help(T){
          return `<div class="card"><p style="margin-top:0">${esc(T.helpText)}</p><div class="actions"><button class="b" onclick="api('openDataFolder')">${esc(T.dataFolder)}</button>${S.paths.errorLog?`<button class="b" onclick="api('openErrorLog')">${esc(T.errorLog)}</button>`:''}<button class="b" onclick="api('openIssues')">${esc(T.issue)}</button><button class="b" onclick="api('openRepo')">${esc(T.project)}</button></div></div>
          ${S.canUninstall?`<section><h2>${esc(T.uninstall)}</h2><div class="card"><span class="path">${esc(S.paths.data)}</span>
          <div class="actions"><button class="b danger" onclick="uninstall(false)">${esc(T.uninstall)} · ${esc(T.keepData)}</button><button class="b danger" onclick="uninstall(true)">${esc(T.uninstall)} · ${esc(T.deleteData)}</button></div></div></section>`:''}`;
        }
        function closedView(){stopped=true;$('#app').innerHTML='<div class="card">'+esc(S.txt.closed)+'</div>';$('#foot').innerHTML='';$('#reportPane').classList.add('hidden')}
        async function uninstall(all){if(!confirm(S.txt.uninstallConfirm))return;await api('uninstall',all);closedView()}
        async function quit(){if(!confirm(S.txt.quitConfirm))return;try{await api('quit')}catch(e){}closedView()}
        refresh(true);setInterval(()=>refresh(false),2000);
        </script></body></html>
        """;
}
