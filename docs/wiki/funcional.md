# Agendia — Nivel funcional

> Qué hace Agendia y qué te resuelve, contado sin tecnicismos. Si gestionas una agenda de
> citas —una peluquería, una clínica, una academia, un taller—, esto es para ti.

← Volver al [índice de la wiki](README.md) · Ver el [nivel técnico](tecnico.md)

---

## Qué es Agendia

Imagina un ayudante que se encarga de toda tu agenda: sabe cuándo trabajas, cuánta gente te
cabe, apunta las reservas, avisa a los clientes y **nunca se lía** con dos personas a la misma
hora. Eso es Agendia.

Tú le dices **cómo trabajas** —tu horario, tus servicios, cuánta gente atiendes a la vez, tus
vacaciones— y a cambio tu agenda **se gestiona sola**: los clientes ven tus huecos reales y
reservan, tú te olvidas del cuaderno, los WhatsApps sueltos y las dobles reservas.

Agendia no es una app que tú abres: es la **maquinaria de reservas** que va por debajo de una
plataforma. Puede dar servicio a **muchos negocios a la vez**, cada uno con su propia agenda
independiente.

## Las piezas

Agendia organiza el mundo en cinco cosas sencillas:

- **Negocio** — tu peluquería, clínica, academia o taller. Cada negocio tiene su agenda propia.
- **Profesional o recurso** — la persona que atiende: peluquero, fisio, profesor… o incluso una
  sala o un equipo que se reserva.
- **Servicio** — lo que se reserva: "corte de pelo 30 min", "clase de guitarra 60 min",
  "revisión 45 min". Lleva su duración y su precio.
- **Cliente** — quien reserva. Puede tener cuenta o ser un registro de mostrador/teléfono.
- **Cita** — la que une todo lo anterior: un cliente, con un profesional, para un servicio, en
  una fecha y hora concretas.

> **Un detalle potente.** Cada profesional o recurso tiene un **aforo**: cuántas reservas admite
> *a la vez*. Un fisio atiende de uno en uno (aforo 1). Un profesor con clase de grupo de 8 o un
> monitor de yoga con 15 plazas tienen aforo mayor. Con esto Agendia modela desde el 1-a-1 hasta
> las clases y actividades de grupo.

## Tu horario base

En lugar de ir marcando huecos libres a mano, defines tu **horario habitual** una vez, y Agendia
calcula sola tu disponibilidad cada día.

> **Ejemplo.** "De septiembre a junio trabajo de lunes a viernes, de 16:00 a 21:00." A partir de
> ahí, la app ya sabe qué huecos tienes cada día. No tienes que marcar "el martes tengo libre a
> las 17h": lo deduce.

Y entiende cosas de la vida real:

- **Turnos partidos.** Mañana y tarde con una pausa en medio ("de 10:00 a 14:00 y de 16:00 a
  20:00") están soportados. Y no dejará que una cita se parta por la mitad del descanso: tiene
  que caber entera en una franja.
- **Temporadas.** El horario del curso puede ser uno y el de verano otro. Tú los defines y la app
  usa el que toca en cada fecha.

## Cambiar el horario cuando quieras

Tu horario habitual está fijo, pero la vida no siempre lo respeta. Por eso puedes **sobreescribir
tu horario para un día o un periodo concreto en cualquier momento**, y tu horario normal se queda
intacto para el resto.

> **Ejemplos.**
> - "Este jueves solo abro de mañana." → ese jueves se muestran solo los huecos de la mañana.
> - "La semana de exámenes atiendo de 18:00 a 22:00 en vez de por la tarde." → esa semana manda el
>   horario nuevo; la siguiente vuelve sola a tu horario de siempre.
> - "El viernes 14 cierro." → ese día queda cerrado y nadie puede reservar.

La idea: no rehaces tu horario cada vez que hay una excepción. Pones el cambio para esas fechas y
**la app lo prioriza solo ahí**. Lo que ya estaba reservado sigue en pie; lo nuevo se ajusta al
horario que acabas de poner.

## Festivos y vacaciones

Pones tus festivos y tus periodos de vacaciones, y esos días **se cierran solos**. Nadie podrá
reservar y no tienes que ir rechazando reservas a mano.

> **Ejemplo.** "La semana del 25 al 31 de diciembre estoy de vacaciones" y "el 1 de mayo es
> festivo." Agendia cierra esos días automáticamente y se lo salta al calcular tus huecos.

## Huecos y aforo

Cuando un cliente quiere reservar, ve **exactamente los huecos que tienes libres** ese día, con
las plazas que quedan. No verá huecos que ya pasaron, ni días cerrados, ni las horas que quitaste
con un cambio puntual.

En las actividades de grupo, la app **cuenta las plazas sola**: enseña "quedan 5", luego
"quedan 4"… y **cierra el grupo cuando se llena**. Tú no llevas la cuenta.

> **Ejemplo.** Son las 18:00 del martes. Un cliente mira tu agenda y ve "miércoles 17:00 libre,
> jueves 18:00 quedan 3 plazas en grupo". Reserva el miércoles. Ese hueco desaparece al instante
> para los demás.

## Reservar sin líos

Antes de aceptar una cita, Agendia comprueba que **todo cuadra**: que el día está abierto, que
entra en una franja, que la duración coincide con el servicio, que el profesional está activo y
que **no se supera su aforo**.

Y resuelve el problema clásico de las agendas: **nunca dos clientes en el mismo sitio**. Si dos
personas intentan coger la última plaza en el mismo segundo, solo una la consigue y la otra ve que
ya no está. Sin dobles reservas, sin sustos.

## Citas fijas (series)

¿Tienes un cliente que viene **todos los martes a las 17:00 hasta final de curso**? No lo apuntas
semana a semana. Lo creas de golpe:

> **Ejemplo.** "Todos los martes a las 17:00, hasta el 30 de junio." La app crea todas las citas
> del tirón. Si alguna cae en festivo, en tus vacaciones o en un hueco lleno, la **salta y te
> avisa**. Y si el cliente se da de baja, cancelas **toda la serie** de una vez (o la mueves entera
> a otra hora).

Admite patrones semanales (varios días), cada dos semanas, o mensuales por día del mes.

## Lista de espera

Si una franja está **completa** y alguien más quiere entrar, se apunta a la lista de espera.
Cuando un cliente cancela, Agendia **avisa automáticamente al primero de la cola**. El hueco no se
pierde: se llena solo, y por orden de llegada.

## Menos plantones (esto ahorra dinero)

Los huecos que se quedan vacíos a última hora son dinero perdido. Agendia ataca eso por tres vías:

- **Recordatorios automáticos.** El cliente recibe un aviso el día antes de su cita. Menos olvidos.
- **Regla de cancelación.** Tú decides: "para cancelar hay que avisar con 24 h de antelación". Más
  tarde, el cliente ya no puede cancelar solo. Tú siempre puedes mover lo que quieras.
- **Lista de espera.** Si alguien cancela, el hueco se ofrece automáticamente a quien esperaba.

## Aviso de retraso

Si un día vas con retraso, avisas en un clic. La app avisa **solo a los clientes del tramo
afectado** (por ejemplo, los de la tarde), no a todos. Nadie se planta en la puerta esperándote
sin saberlo.

## Varios servicios a la vez

Una misma cita puede combinar **varios servicios** —por ejemplo "corte + color", o "clase +
alquiler de sala"— y Agendia **suma las duraciones** para reservar el tiempo correcto.

## Estadísticas

Sin llevar cuentas a mano, ves cómo va tu negocio:

- Cuántas citas tienes por semana y por mes.
- Qué servicios se piden más y cuáles menos (con sus ingresos).
- Cuánta gente falta o cancela, y en qué porcentaje.
- A qué horas y días estás más cargado y cuáles tienes más flojos.

Eso te ayuda a decidir con datos: por ejemplo, mover la clase de grupo del viernes a la tarde
porque a esa hora siempre se llena.

## Quién puede hacer qué

Agendia distingue cuatro tipos de usuario:

| Perfil | Qué puede hacer |
|---|---|
| **Administrador** | Gestiona toda la plataforma: puede ver y tocar cualquier negocio. |
| **Dueño del negocio** | Manda en *su* negocio: horarios, servicios, empleados, citas, estadísticas. |
| **Empleado** | Trabaja en un negocio: gestiona citas y ve la agenda de su negocio. |
| **Cliente** | Reserva, ve sus propias citas, y cancela/reprograma las suyas (según la regla de antelación). |

Cada quien ve y toca **solo lo suyo**: un dueño no ve la agenda del negocio de al lado, y un
cliente solo gestiona sus propias citas.
