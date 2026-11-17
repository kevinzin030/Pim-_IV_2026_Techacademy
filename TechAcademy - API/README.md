
# TechAcademy - API REST

## Sobre o projeto

API REST desenvolvida em Node.js e Express para o projeto acadêmico TechAcademy (PIM IV), com o objetivo de centralizar as regras de negócio e permitir a comunicação entre as aplicações Desktop, Web e Mobile.

## Tecnologias utilizadas

- Node.js
- Express
- JavaScript
- SQL Server
- Azure (implantação planejada)

## Estrutura do projeto

- `Src/Config` - Configurações da aplicação
- `Src/Controllers` - Controladores da API
- `Src/Middlewares` - Middlewares
- `Src/Routes` - Definição das rotas
- `Src/server.js` - Inicialização do servidor
- `Prisma` - Diretório reservado à integração com o banco de dados

## Como executar

Instale as dependências:

```bash
npm install
```

Inicie o servidor em desenvolvimento:

```bash
npm run dev
```

## Endpoint implementado

**GET /api/health**

Endereço local:

`http://localhost:3000/api/health`

Retorna uma resposta JSON indicando o funcionamento da API.

## Status do desenvolvimento

A estrutura inicial da API REST está criada e o servidor Express já pode ser executado localmente.

As próximas etapas incluem a implementação dos endpoints, a integração com o SQL Server e a implantação na Azure.
