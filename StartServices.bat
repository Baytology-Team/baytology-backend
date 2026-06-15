@echo off
echo Starting Baytology Environment...

echo Starting RabbitMQ...
cd /d "f:\baytology-backend"
docker-compose -f docker-compose.rabbitmq.yml up -d

echo Starting Chatbot System (Port 8000)...
start "Baytology Chatbot" cmd /k "cd /d F:\GraduationProject\Baytology-chatbot-system && python -m uvicorn api:app --reload --port 8000"

echo Starting Recommendation System (Port 8002)...
start "Baytology RecSys" cmd /k "cd /d F:\GraduationProject\Baytology-feat-recommendation-system && python -m uvicorn app:app --reload --port 8002"

echo Starting Image Search API (Port 8001)...
start "Baytology Image Search" cmd /k "cd /d F:\GraduationProject\Search_By_Image\src && python -m uvicorn api:app --host 0.0.0.0 --port 8001"

echo Starting .NET API Backend (Port 5001/7053)...
start "Baytology API" cmd /k "cd /d f:\baytology-backend\src\Baytology.Api && dotnet run"

echo Starting Angular Frontend (Port 4200)...
start "Baytology Frontend" cmd /k "cd /d f:\baytology-backend\Test-1 && npm start"

echo All services launched in separate windows!
pause
