# Plano técnico de la casa y la parcela (a escala)

Unidades en metros. Mundo de Unity: **x** hacia el este, **z** hacia el norte, **y** arriba. La calle está al sur, el patio al norte. Suelo interior a **y = 0.45** (losa elevada, tres peldaños en el porche).

## Parcela y calle

| Elemento | x | z | Notas |
|---|---|---|---|
| Calzada | −30 → 48 | −9 → −2.15 | asfalto, dos sentidos |
| Bordillo | — | −2.15 → −2.0 | 0.15 de alto |
| Acera | −30 → 48 | −2.0 → 0 | baldosa de hormigón, alcorques |
| Parcela | 0 → 18 | 0 → 26 | |
| Entrada de coches | 0.6 → 3.8 | 0 → 13 | hormigón ranurado |
| Coche de Daniel | centro 2.2 | 2.6 → 7.0 | morro al norte |
| Valla y **portón** | 0 → 4.5 | z = 13 | portón peatonal x 1.6 → 2.8, pestillo; ruta B |
| Valla lateral este (cerrada) | 14.5 → 18 | z = 7 | sin salida, vecino |
| Patio trasero | 0 → 18 | 18 → 26 | césped, losetas, tendedero |
| Cobertizo | 14.2 → 17.2 | 21.6 → 24.6 | puerta única al oeste, hacia el patio |

## Casa (huella x 4.5 → 14.5, z 6 → 18)

Coordenadas locales de la casa: X = x − 4.5, Z = z − 6.

| Espacio | X | Z | Tamaño | Contenido clave |
|---|---|---|---|---|
| Porche | 0.8 → 3.8 | −1.6 → 0 | 3.0 × 1.6 | lámpara, felpudo, macetas |
| Sala / entrada | 0 → 5.2 | 0 → 4.5 | 5.2 × 4.5 | puerta principal X 1.6 → 2.5, sofá, TV, fotos |
| Comedor | 5.2 → 10 | 0 → 4.5 | 4.8 × 4.5 | abierto a la sala, mesa |
| Pasillo | 4.0 → 5.2 | 4.5 → 12 | 1.2 de ancho | une sala, dormitorios, baño y cocina |
| Segundo dormitorio | 0 → 4.0 | 4.5 → 8.3 | 4.0 × 3.8 | cuarto de costura / trastos |
| Dormitorio de Elena | 0 → 4.0 | 8.3 → 12 | 4.0 × 3.7 | cómoda (carpeta azul), mesita (cargador), armario (ropa) |
| Baño | 5.2 → 7.4 | 9.6 → 12 | 2.2 × 2.4 | |
| Cocina | 5.2 → 10 | 4.5 → 9.0 | 4.8 × 4.5 | nevera, fregadero, mesa, calendario |
| Lavadero | 7.4 → 10 | 9.0 → 12 | 2.6 × 3.0 | **escalón de 0.18 hacia abajo** desde la cocina (caída de Elena); lavadora, estanterías; puerta de servicio al norte (cierre defectuoso) |

## Rutas (CASA_Y_FLUJOS §Rutas)

- **A**: dormitorio → pasillo → sala → puerta principal → porche → acera.
- **B**: cocina → escalón → lavadero → puerta de servicio → patio → portón (x 1.6 → 2.8, z 13) → entrada de coches → calle.
- **C**: cobertizo → patio → lavadero → cocina → sala (si las puertas están abiertas).

Ninguna ruta obliga a cruzarse con Víctor cuando existe otra salida física disponible: A y B no comparten tramos interiores.

## Encuadre del menú

Cámara en la acera suroeste (≈ −0.8, 1.5, −1.2) mirando al porche (yaw ≈ 50°): coche en primer plano a la izquierda, fachada iluminada a la derecha, como en el moodboard.
