# Guia de contribuição

Obrigado por contribuir com o TechAcademy — PIM IV.

## Fluxo de trabalho

1. Atualize sua branch local a partir de `develop`.
2. Crie uma branch curta e descritiva: `feature/nome-da-funcionalidade`, `fix/correcao` ou `docs/documentacao`.
3. Faça commits pequenos, com mensagens claras.
4. Teste as alterações localmente.
5. Abra um Pull Request para `develop` e descreva o que mudou, como testar e quais requisitos atende.
6. Solicite revisão de outro integrante antes de integrar.

A branch `main` deve conter versões estáveis. Não envie alterações diretamente para `main`.

## Convenção de commits

Prefira mensagens no padrão:

- `feat: adiciona consulta de frequência`
- `fix: corrige validação de login`
- `docs: atualiza instruções da API`
- `refactor: organiza serviço de autenticação`
- `test: adiciona testes de consulta`
- `chore: ajusta configuração do projeto`

## Qualidade e segurança

- Não envie senhas, tokens, chaves privadas, dados reais de alunos ou arquivos `.env`.
- Use dados fictícios nos exemplos e testes.
- Atualize a documentação quando uma configuração ou comportamento mudar.
- Informe no Pull Request os testes executados e limitações conhecidas.
