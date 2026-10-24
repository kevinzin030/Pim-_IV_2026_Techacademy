# TechAcademy — PIM IV

> Projeto acadêmico de integração entre plataforma web, aplicação desktop, aplicativo mobile e API para consulta e gestão de informações acadêmicas.

## Visão geral

O TechAcademy reúne módulos que se comunicam por uma API central. A plataforma web existente será **reaproveitada e evoluída**, preservando seu repositório original. Os módulos desktop, mobile e API serão desenvolvidos conforme o escopo do PIM IV.

## Módulos

| Módulo | Tecnologia prevista | Situação inicial |
|---|---|---|
| Web |   HTML + CSS + JAVASCRIPT | Desenvolver |
| API | Node.js + Express | Desenvolver |
| Desktop | C# + Windows Forms | Desenvolver |
| Mobile | Android | Desenvolver |
| Banco de dados | A definir conforme o modelo do projeto | Documentar e integrar |
| Documentação | Markdown e diagramas UML | Em organização |

## Estrutura do repositório

```text
pim/
├── api/                  # API Node.js + Express
├── desktop/              # Aplicação C# Windows Forms
├── mobile/               # Aplicativo Android
├── database/             # Scripts e documentação do banco
├── docs/
│   ├── arquitetura/
│   ├── requisitos/
│   ├── scrum/
│   └── diagramas/
├── .github/              # Modelos de Issues e Pull Requests
├── .gitignore
└── README.md
```

As pastas dos módulos serão preenchidas conforme o desenvolvimento avançar. O código web original continua no repositório próprio; sua integração será documentada sem sobrescrever os arquivos existentes.

## Escopo funcional previsto

- Professores lançam notas pela plataforma web.
- Frequência é registrada pela plataforma web, conforme as regras definidas para o projeto.
- Usuários consultam notas e frequência no aplicativo mobile.
- Aplicação desktop oferece as funcionalidades administrativas definidas nos requisitos.
- API centraliza a comunicação entre os clientes e os dados acadêmicos.
- Acesso aos dados deve respeitar autenticação, autorização e proteção de informações pessoais.

> O comportamento final de cada módulo deve seguir os requisitos aprovados e o manual do PIM IV.

## Equipe Scrum

- **Product Owner:** Kevin Luiz
- **Scrum Master:** Max Vinicius
- **Desenvolvimento:** Pedro Bastos, Maria Fernanda e Josué Abraão

## Prazo acadêmico

**Entrega prevista: 22 de novembro de 2026.**

## Começando

1. Consulte os documentos em `docs/`.
2. Leia a documentação específica de cada módulo antes de executar o projeto.
3. Configure variáveis locais a partir de arquivos `.env.example`, quando existirem.
4. Nunca envie senhas, tokens, chaves de API, dados pessoais reais ou arquivos `.env` para o Git.

Os comandos de instalação e execução serão adicionados em cada módulo quando sua implementação estiver disponível.

## Fluxo de trabalho Git

- `main`: versão estável e apresentável.
- `develop`: integração do trabalho em andamento (criar quando a equipe começar a usá-la).
- `feature/nome-da-tarefa`: novas funcionalidades.
- `fix/nome-do-problema`: correções.
- Use Issues para registrar tarefas e Pull Requests para revisar alterações relevantes.
- Escreva mensagens de commit claras, por exemplo: `feat(api): cria rota de consulta de notas`.

## Documentação

- [Organização e arquitetura](docs/README.md)
- [Arquitetura proposta](docs/arquitetura/visao-geral.md)
- [Requisitos](docs/requisitos/README.md)
- [Scrum e planejamento](docs/scrum/README.md)
- [Banco de dados](database/README.md)

## Aviso

Este é um projeto acadêmico. Não publique dados reais de alunos, credenciais, informações sensíveis ou configurações privadas. A visibilidade pública do repositório significa que o código e a documentação adicionados aqui poderão ser vistos por qualquer pessoa.
