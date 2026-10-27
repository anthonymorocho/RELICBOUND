# Ataque y máquinas (P4 y P7)

## Estructura de la batalla
Se carga **solo** la fortaleza defensora más las máquinas y unidades del atacante. Hay una zona de despliegue marcada en un borde del mapa. Las defensas actúan solas.

## Apuntado libre
- Se elige la máquina y luego se toca o se hace clic en cualquier punto del terreno o de una estructura dentro del alcance.
- Un arco de trayectoria previsualizado muestra el punto de caída y el radio de error.
- **Potencia:** en PC, mantener pulsado el botón izquierdo y soltar; en Android, mantener el botón de disparo y soltar. La potencia ajusta la distancia a lo largo del arco.
- La simulación balística es determinista (paso fijo de 50 Hz, sin `Rigidbody` para el proyectil lógico) para poder validarla en el servidor más adelante. El proyectil visual sigue la trayectoria lógica.

## Máquinas
| Máquina | Rol | Alcance | Recarga | Daño a estructura | Fase |
|---|---|---|---|---|---|
| Catapulta | Equilibrada | 60 m | 6 s | 100 | P4 |
| Trabuquete | Demolición | 90 m | 12 s | 260 | P7 |
| Ballesta pesada | Precisión, anti-máquina | 70 m (tiro tenso) | 4 s | 40 (×3 contra máquinas) | P7 |
| Ariete | Solo puertas | Cuerpo a cuerpo | 2 s por golpe | 150 contra puertas | P7 |
| Zapadores | Abren pasos y desactivan trampas | — | — | — | P7 |
| Torre de asedio | Superar muralla | — | — | — | Después de la 1.0 |

## Municiones
| Munición | Ventaja | Desventaja |
|---|---|---|
| Piedra estándar | Fiable | — |
| Piedra pesada | +60 % de daño a estructuras | −25 % de alcance, recarga +30 % |
| Incendiaria | Daño continuo a madera y unidades | −50 % contra piedra |
| Perforante | Ignora la armadura de las torres | Radio de impacto pequeño |
| Dispersión | Área contra unidades | Daño a estructuras casi nulo |

## Control táctico
Seleccionar máquina · apuntar · elegir munición · disparar · cambiar de máquina · desplegar unidades en la zona · orden de avance · marcar un objetivo prioritario · ver el estado del asedio (porcentaje de destrucción y objetivos).
