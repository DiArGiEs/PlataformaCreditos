# Plataforma de Créditos - ASP.NET Core 10 MVC

Sistema web para la gestión e integración asíncrona de solicitudes de crédito con evaluación de riesgo, notificaciones en tiempo real vía WebSockets e integración con Cloud MQ (RabbitMQ).

---

## 🛠️ Tecnologías Utilizadas
- **Framework:** ASP.NET Core MVC (.NET 10)
- **Base de Datos:** SQLite / Entity Framework Core 10
- **Autenticación & Roles:** ASP.NET Core Identity (`Cliente`, `Analista`)
- **Tiempo Real:** SignalR / WebSockets
- **Mensajería Asíncrona:** RabbitMQ / CloudAMQP (`RabbitMQ.Client`)
- **Contenedores & Despliegue:** Docker, Render.com

---

## 🚀 Configuración y Ejecución Local

1. **Clonar el repositorio:**
   ```bash
   git clone [https://github.com/DiArGiEs/PlataformaCreditos.git](https://github.com/DiArGiEs/PlataformaCreditos.git)
   cd PlataformaCreditos