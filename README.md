# IA Back .NET Creator

Sim — este projeto agora cria uma API que usa IA para **planejar e também gerar arquivos iniciais de aplicações .NET**.

## O que esta API faz

### 1) `POST /api/blueprint`
Recebe uma ideia e devolve um blueprint técnico com:
- arquitetura sugerida;
- estrutura de pastas;
- endpoints/fluxos;
- modelo de dados inicial;
- plano em sprints;
- riscos e mitigação.

### 2) `POST /api/generate-files`
Recebe a ideia do produto e devolve:
- resumo técnico da solução;
- lista de arquivos (`path` + `content`) para iniciar o projeto;
- próximos passos sugeridos.

> Isso responde ao objetivo principal: usar IA para começar a construir aplicações em .NET de forma assistida.

## Stack

- .NET 8 (Minimal API)
- Swagger (documentação interativa)
- OpenAI Responses API

## Como rodar localmente

> Pré-requisito: .NET 8 SDK instalado.

1. Configure a chave da OpenAI:

```bash
export OPENAI_API_KEY="sua-chave"
```

2. Execute a API:

```bash
dotnet run --project src/AiDotnetBuilder.Api/AiDotnetBuilder.Api.csproj
```

3. Abra o Swagger:

- `http://localhost:5000/swagger` (ou porta mostrada no terminal)

## Exemplo: gerar blueprint

`POST /api/blueprint`

```json
{
  "goal": "Criar um SaaS de gestão de clínicas",
  "domain": "Saúde",
  "stack": "ASP.NET Core Web API + React + PostgreSQL",
  "constraints": "LGPD, multi-tenant, deploy em Azure",
  "outputLanguage": "pt-BR"
}
```

## Exemplo: gerar arquivos iniciais

`POST /api/generate-files`

```json
{
  "goal": "Criar um sistema de agendamento para clínicas",
  "domain": "Saúde",
  "stack": "ASP.NET Core Web API + React + PostgreSQL",
  "constraints": "Autenticação JWT, multi-tenant",
  "outputLanguage": "pt-BR",
  "maxFiles": 10
}
```

## Próximos passos recomendados

1. Criar endpoint para salvar os arquivos gerados em disco/repositório Git.
2. Adicionar validação de segurança dos arquivos antes de persistir.
3. Incluir templates próprios por tipo de projeto (SaaS, ERP, e-commerce).
4. Acoplar testes automáticos para cada scaffold gerado.
5. Evoluir para geração incremental por módulo.
