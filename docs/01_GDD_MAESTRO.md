# GDD maestro

## Concepto
Cada jugador construye y mejora **su propia fortaleza medieval de fantasía**, prepara un ejército ofensivo y una defensa automática, y asedia fortalezas de la IA (campaña) o de otros jugadores (PvP asíncrono). Lo que lo diferencia: **apuntado libre con destrucción física que cambia el campo de batalla**.

## Pilares (ordenados por prioridad cuando entren en conflicto)
1. **Mi castillo es mío.** Pantalla principal, personalizable, evoluciona a la vista con cada mejora.
2. **Destruir se siente bien.** Cada impacto deja huella; las brechas son reales y cambian rutas.
3. **Decidir, no mirar.** El atacante elige máquina, munición, punto de impacto y momento de desplegar tropas.
4. **Progreso justo.** Nunca se pierde progreso permanente; nunca se compra poder.

## Bucle principal
```
Castillo ──(Atacar)──► Preparación ──► Asedio ──► Resultado ──► Recompensas
   ▲                                                              │
   └──────(Mejorar edificio / investigar / entrenar)◄─────────────┘
```
Duración objetivo: un asedio dura entre 2 y 4 minutos y una sesión típica en móvil entre 8 y 15 minutos (2–3 asedios + mejoras).

## Flujo cronológico del jugador (orden de desarrollo)
| Paso | Momento | Fase | Sistema |
|---|---|---|---|
| 1 | Primera apertura | P1 | `BootFlow.Splash` |
| 2 | Carga breve y marca (≤ 3 s) | P1 | Escena `Boot` |
| 3 | Crear o cargar perfil local | P1 | `SaveService`, `BootFlow.LoadingSave` |
| 4 | Nombre del jugador y del castillo | P1 | `NameValidator`, pantalla de perfil |
| 5 | Introducción contextual (≤ 20 s, se puede saltar) | P1/P2 | `BootFlow.Intro` |
| 6 | Aparece el castillo inicial | P1 | Escena `Castle` |
| 7 | Tutorial de cámara y selección | P2 | Tutorial contextual |
| 8 | Primera construcción o mejora | P3 | Construcción |
| 9 | Presentación del taller de asedio | P4 | Taller |
| 10–14 | Primer ataque PvE: preparar, disparar, brecha, entrada de tropas, resultado | P4 | Combate y destrucción |
| 15 | Recompensas | P5 | Economía |
| 16–17 | Vuelta al castillo y primera mejora pagada con esas recompensas | P5 | Mejoras |
| 18 | Apertura progresiva de sistemas | P5–P9 | Desbloqueos por nivel (`12`) |
| 19 | Campaña, investigación, expansión | P6–P9 | |
| 20 | Competición y temporadas | P11–P12 | Solo con backend real |

## Alcance de la versión 1.0
**Dentro:** 5 regiones de campaña, 4 máquinas (catapulta, trabuquete, ballesta pesada, ariete), 4 municiones, 5 unidades ofensivas, 5 defensores, nivel máximo 30, PvP asíncrono por plataforma, ligas, temporadas de 8 semanas, tienda de cosméticos.
**Fuera de 1.0:** clanes, torre de asedio móvil, crossplay, PvP en tiempo real, editor de mapas compartible.

## Glosario
- **Fortaleza / castillo del jugador:** toda la base. **Castillo central:** el edificio principal; su nivel limita al resto.
- **Asedio:** una batalla. **Brecha:** hueco transitable abierto en muralla o puerta.
- **Sección:** módulo destructible de una muralla (ver `08`).
