
# API — Node.js e Express

A API será desenvolvida para centralizar as regras de negócio e a comunicação entre as aplicações web, desktop e mobile.

## Estado atual

A estrutura inicial da API foi criada utilizando Node.js e Express. O servidor local está funcionando e possui uma rota de verificação de funcionamento (`/api/health`).

## Responsabilidades previstas

- Autenticação e autorização.
- Consulta de alunos, turmas, notas e frequência conforme permissões.
- Endpoints consumidos pelas aplicações web, desktop e mobile.
- Validação de dados e respostas de erro consistentes.

## Configuração

A API utiliza Node.js e Express.

Para instalar as dependências:

```bash
npm install
```

Para iniciar o servidor em desenvolvimento:

```bash
npm run dev
```

A API estará disponível em:

```text
http://localhost:3000
```

Rota de teste:

```text
GET /api/health
```

As configurações sensíveis devem permanecer no arquivo `.env`, que não deve ser enviado ao GitHub.

## Convenções

- Validar entradas no servidor.
- Não confiar em identificadores ou permissões enviados pelo cliente.
- Não retornar dados acadêmicos sem autorização.
- Criar testes para fluxos críticos.
