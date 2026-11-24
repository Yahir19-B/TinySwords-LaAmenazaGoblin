# Tiny Swords: La Amenaza Goblin

Videojuego 2D de acción y aventura hecho en **Unity 6**. Eres el **Caballero Azul**, el último guerrero del reino: cruza 10 tierras malditas, derrota a los goblins, junta oro para mejorar tus armas y enfrenta al **Gran Jefe Goblin** en su guarida.

## ▶ Jugar en el navegador

**https://yahir19-b.github.io/TinySwords-LaAmenazaGoblin/**

Funciona en computadora con Chrome, Edge, Firefox o Safari (teclado y mouse). La primera carga tarda unos segundos. El progreso se guarda en tu navegador.

## 📱 Descargar para Android

**https://github.com/Yahir19-B/TinySwords-LaAmenazaGoblin/releases/latest/download/TinySwords.apk**

Android 8 o superior, procesador de 64 bits. Abre el archivo en el celular, permite instalar de esa fuente y toca Instalar. Se juega con palanca y botones en la pantalla.

## Controles

| Tecla | Acción |
|---|---|
| W A S D / Flechas | Moverse |
| Espacio / J / Clic | Atacar con la espada |
| Shift / K | Esquivar (no recibes daño) |
| Q | Torbellino (se compra en la Tienda) |
| R | Poción de vida (se compra en la Tienda) |
| Esc | Pausa |
| H | Ver los controles |
| M | Silenciar (el volumen se ajusta en el menú y en la pausa) |

## El juego

- **10 niveles** que se desbloquean al ganar, cada uno más oscuro y difícil, y un **jefe final**.
- **7 tipos de enemigos**: goblins con antorcha, veloces, con dinamita, bombarderos, arqueros malditos, barriles kamikaze y caballeros oscuros, además de goblins **élite**.
- **Tienda** con mejoras (espada, armadura, botas), poderes (Torbellino y Poción) y 4 skins del caballero.
- Combos, golpes críticos, barriles explosivos, trampas de fuego, emboscadas y tesoros escondidos.

Para ganar un nivel hay que derrotar a todos los goblins y destruir sus casas.

## Abrir el proyecto en Unity

1. Instala **Unity 6000.6.0f1** desde Unity Hub.
2. Descarga gratis el pack **[Tiny Swords de Pixel Frog](https://pixelfrog-assets.itch.io/tiny-swords)**. Su licencia no permite redistribuir los archivos originales, por eso no vienen en este repositorio.
3. Copia el contenido del pack en `Assets/images/Tiny Swords (Update 010)/` (y el *Free Pack* en `Assets/images/Tiny Swords (Free Pack)/`), respetando las carpetas. Los archivos `.meta` que ya están en el repositorio conservan los cortes de los sprites.
4. Abre el proyecto en Unity Hub y luego la escena `Assets/Scenes/MainMenu.unity`.

El menú **Tiny Swords** del editor tiene las herramientas del proyecto: abrir escenas, regenerar niveles y compilar para navegador (WebGL, en la carpeta `docs/`) y para Android (APK, en `Builds/Android/`).

## Equipo

- **Yahir Baltazar Aviles**: desarrollo
- **Valeria Arriga Jaimes**: narrativa y documentación
- **José María Patricio Hernández**: arte, audio y pruebas

Proyecto de la materia *Creación de videojuegos*, grupo 1002, Universidad Tecnológica del Sur del Estado de México.

## Créditos

- Arte: **[Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords)** por Pixel Frog.
- Música y efectos: generados por código con `Tools/audio/generar_audio.py` (sin muestras externas).
- Motor: Unity 6 con Universal Render Pipeline 2D.
