# PvP y clasificaciones (P11–P12 — deshabilitado hasta que exista backend autorizado)

## Asíncrono
Elegir rival → descargar una **instantánea validada** de su castillo y defensas → asedio contra la IA defensiva → enviar el **registro de entradas** (disparos con tick, ángulo y potencia; despliegues) → el servidor re-simula y decide el resultado → informe al defensor.

El cliente **nunca** envía «he ganado con X puntos». Envía las entradas y el servidor calcula el resultado. Por eso la simulación de combate tiene que ser determinista (paso fijo, sin `Random` no sembrado, física decorativa separada).

## Separación por plataforma
Cuatro tablas independientes: PvE Android, PvP Android, PvE PC y PvP PC. La plataforma se fija en el servidor por la cuenta y la build firmada, no la declara el cliente.

## Equilibrio
Emparejamiento por nivel de fortaleza ±1 y por puntuación de liga. Presupuesto de despliegue por liga (puntos de máquinas y unidades). Validación de castillos imposibles en el servidor. Protección contra granjas de cuentas (límite de ataques entre el mismo par por día).

## Ligas
Bronce, Plata, Oro, Platino, Diamante, Maestro y Leyenda. Puntuación tipo Elo con K variable: un rival más fuerte da más puntos y la pérdida máxima por defensa es de 20 puntos.

## Mientras no haya backend
El botón de PvP muestra el estado «Próximamente» con candado. **No** se presentan bots como jugadores reales.
