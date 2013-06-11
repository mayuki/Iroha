(function(window) {

if (!window.Iroha)
    window.Iroha = {};

Iroha.ComponentEditor = {
    /// <summary>
    /// Iroha: Component Editor
    /// <summary>

    // Properties -----
      allComponents: []
    , allComponentsById: {}
    , componentLoader: null
    , bindingFilters: null

    // Methods -----
    , setup: function () {
        /// <summary>
        /// Initialize Component Editor
        /// <summary>

        this.componentLoader = this.componentLoader || Iroha.ComponentEditor.ComponentLoader.Embedded;
        this.bindingFilters = this.bindingFilters || Iroha.ComponentEditor.DefaultBindingFilters;

        this.allComponents = this.loadComponents();
        for (var i = 0; i < this.allComponents.length; i++) {
            this.allComponentsById[this.allComponents[i].id] = this.allComponents[i];
        }
    }

    , loadComponents: function() {
        /// <summary>
        ///
        /// </summary>
        return this.componentLoader.load();
    }

    , getContainableComponents: function(parentComponent) {
        var components = [];
        for (var i = 0; i < this.allComponents.length; i++) {
            if (parentComponent.isContainable(this.allComponents[i])) {
                components.push(this.allComponents[i]);
            }
        }
        return components;
    }
    
    , createContentDataFromHtml: function(htmlFragment) {
        /// <summary>
        /// HTML -> ContentData
        /// </summary>
        var self = this;
        var components = this.allComponents;
        var componentsById = this.allComponentsById;

        var htmlFragmentNode = $('<div />').html(htmlFragment);
        var contentData = [];
        htmlFragmentNode.find('> [data-iroha-component]').each(function (i, e) {
            var componentNode = $(e);
            var componentNodeWrap = $('<div />').html(componentNode);
            var contentDataEntry = {
                id      : componentNode.data('iroha-component'),
                children: [],
                data    : []
            };

            var component = componentsById[contentDataEntry.id];
            component.bindings.forEach(function (binding, i) {
                // filter (fromHtml)
                var filters = (binding.filters || 'html').split(/\s*\|\s*/).map(function (e) { return self.bindingFilters[e]; }).filter(function (e) { return e; });
                var processFilters = function (v) {
                    filters.forEach(function(e) { v = e.fromHtml(v); });
                    return v;
                }

                // data-iroha-component-idのついた要素にbindがついている可能性があるのでdivで囲んだものから探し出す
                var bindTarget = componentNodeWrap.find(binding.selector);
                var content = null;
                if (binding.attrName) {
                    content = processFilters(bindTarget.attr(binding.attrName));
                } else {
                    content = processFilters(bindTarget.html());
                }
                contentDataEntry.data.push({ id: binding.id, content: content });
            });

            // children
            componentNode.find('[data-iroha-component]').each(function (i, child) {
                // 直接の子孫?
                var childNode = $(child);
                if (childNode.parents('[data-iroha-component]').get(0) == componentNode.get(0)) {
                    contentDataEntry.children.push(self.createContentDataFromHtml(childNode)[0]); // 必ず1つ
                }
            });

            contentData.push(contentDataEntry);
        });

        return contentData;
    }

    , createHtmlFromContentData: function (contentData, finalize) {
        /// <summary>
        /// ContentData -> HTML
        /// </summary>
        /// <returns>HTML Fragment</returns>
        var components = this.allComponents;
        var componentsById = this.allComponentsById;
        var self = this;

        return contentData.map(function (e, i) {
            var component = componentsById[e.id];
            var generatedContent = $('<div />').html(component.template);

            // generatedContentは必ず一つの要素だけを持つ
            if (generatedContent.children().length != 1) throw "テンプレートの最上位には一つの要素のみ含むことができます。";
            if (!finalize) {
                generatedContent.children(0).attr('data-iroha-component', component.id);
            }

            if (component.contentTypeMap.length > 0 /*component.type == "Container"*/) {
                generatedContent.find(component.bindings[0].selector).html(self.createHtmlFromContentData(e.children, finalize));
            } else {
                e.data.forEach(function (e, i) {
                    var contentDataEntry = e;
                    var binding = component.bindingsById[contentDataEntry.id];

                    // filter (toHtml)
                    var filters = (binding.filters || 'html').split(/\s*\|\s*/).map(function (e) { return self.bindingFilters[e]; }).filter(function (e) { return e; });
                    var processFilters = function (v) {
                        filters.forEach(function(e) { v = e.toHtml(v); });
                        return v;
                    }

                    generatedContent.find(binding.selector).each(function (i2, e2) {
                        if (binding.attrName) {
                            $(e2).attr(binding.attrName, processFilters(contentDataEntry.content));
                        } else {
                            $(e2).html(processFilters(contentDataEntry.content));
                        }
                    });
                });
            }

            return generatedContent.html();
        }).join('').replace(/<((?:img|br|link|hr)[^>]*?)(\s*\/)?>/g, '<$1 />'); // XHTML
    }

    , createContentDataFromDocumentModel: function(rootNode) {
        /// <summary>
        /// ComponentEditor(DOM) -> Data
        /// </summary>
        /// <returns>HTML Fragment</returns>
        var components = $(rootNode || '#iroha-component-editor').find('> .component-container > .component');
        var contentData = [];
        var self = this;
        components.each(function (i, e) {
            contentData.push({
                id      : $(e).data('iroha-component-id'),
                children: self.createContentDataFromDocumentModel(e),
                data    : self.createComponentDataFromDocumentModel(e)
            });
        });

        return contentData;
    }
    , createComponentDataFromDocumentModel: function(node) {
        /// <summary>
        /// ComponentEditor(DOM) -> ContentData
        /// </summary>
        /// <returns>HTML Fragment</returns>
        var componentData = [];
        $(node).find('*[data-iroha-component-content]').each(function (i, e) {
            var targetNode = $(e);
            var contentId = targetNode.data('iroha-component-content');
            var content = targetNode.val();

            componentData.push({ id: contentId, content: content });
        });

        return componentData;
    }
}


Iroha.ComponentEditor.DefaultBindingFilters = {
    // そのまま出力する
      raw: {
        fromHtml: function (html) { return html; },
        toHtml: function (value) { return value; }
    }

    // 改行をp/br要素に変換する
    , paragraph: {
        fromHtml: function (html) {
            var paragraphs = $('<div />').html(html).find('p');
            var lines = [];
            paragraphs.each(function (i, e) { var pHtml = $(e).html().replace(/<br(\s*|[^>]+)>/g, "\r\n"); lines.push($('<div />').html(pHtml).text()); });

            return lines.join("\r\n\r\n");
        },
        toHtml: function (value) {
            return '<p>' + $('<div />').text(value).html().replace(/^\s*|\s*$/g, '').replace(/(\r?\n){2,}/g, '</p><p>').replace(/\r?\n/g, '<br />') + '</p>';
        }
    }

    // デフォルトのフィルタ
    , html: {
        fromHtml: function (html) { return $('<div />').html(html).text(); },
        toHtml: function (value) { return $('<div />').text(value).html(); }
    }
}


Iroha.ComponentEditor.ComponentLoader = {
    Embedded: {
        load: function () {
            var componentsNode = $('script[type="application/x-iroha-component"]');
        
            return componentsNode.map(function (i, e) {
                var component = $(e);
                var uiTemplate = component.text().match(/<UITemplate(?:\s[^>]*)?>\s*([\s\S]*)\s*<\/UITemplate>/)[1];
                var template = component.text().match(/<Template(?:\s[^>]*bindings="([^"]*)"[^>]*)?>\s*([\s\S]*)\s*<\/Template>/);
                var templateBody = template[2];
                var bindings = template[1];
                return new Iroha.ComponentEditor.Component(
                    component.data("iroha-component-type"),
                    component.data('iroha-component'),
                    component.data("iroha-component-name"),
                    component.data("iroha-component-content-type"),
                    templateBody,
                    uiTemplate,
                    bindings,
                    component.data("iroha-component-hidden")
                );
            });
        }
    }
}; 

Iroha.ComponentEditor.Component = function (type, id, name, contentType, template, uiTemplate, bindings, isHidden) {
    this.type        = type;
    this.id          = id;
    this.name        = name;
    this.contentType = contentType || "";
    this.template    = template;
    this.uiTemplate  = uiTemplate;
    this.bindings    = this.parseBindings(bindings);
    this.isHidden    = isHidden;

    this.bindingsById = {};
    this.bindings.forEach($.proxy(function (e) { this.bindingsById[e.id] = e; }, this));

    this.contentTypeMap = [];
    var contentTypeMapParts = this.contentType.replace(/^\s*|\s*$/g, '').split(/\s*,\s*/);
    for (var i = 0; i < contentTypeMapParts.length; i++) {
        if (contentTypeMapParts[i] != "") {
            var idMatch = contentTypeMapParts[i].match(/#([^#@]+)/);
            var typeMatch = contentTypeMapParts[i].match(/@([^#@]+)/);
            this.contentTypeMap.push({
                id  : (idMatch) ? idMatch[1] : '',
                type: (typeMatch) ? typeMatch[1] : ''
            });
        }
    }
}
Iroha.ComponentEditor.Component.prototype.isContainable = function (childComponent) {
    for (var i = 0; i < this.contentTypeMap.length; i++) {
        var b = ((this.contentTypeMap[i].type) ? this.contentTypeMap[i].type == childComponent.type : true) &&
                ((this.contentTypeMap[i].id) ? this.contentTypeMap[i].id == childComponent.id : true);
        if (b) return true;
    }
    return false;
}
Iroha.ComponentEditor.Component.prototype.parseBindings = function (bindingString) {
    bindingsString = bindingString.replace(/^\s*|\s*$/g, '');
    var bindings = [];
    if (bindingsString != null && bindingsString != '') {
        bindings = bindingsString
                        .split(/\s*,\s*/)
                        .map(function (e, i) {
                            var m = e.match(/^([^=]+)=(.*)/);
                            var selector = m[2].match(/^([^@|]+)(?:@([^|]+))?(?:\|(.+))?/);
                            return {
                                id      : m[1],
                                selector: selector[1],
                                attrName: selector[2],
                                filters : selector[3],
                            };
                        });
    }
    return bindings;
}

Iroha.ComponentEditor.UI = {
      selectors: {
         editor: '#iroha-component-editor'
        ,templateAddComponentBar: '#template-add-component-bar'
        ,templateComponentFrame: '#template-component-frame'
    }

    , setup: function (config) {
        /// <summary>
        ///
        /// </summary>
        config = config || {};

        this.selectors = $.extend(this.selectors, config.selectors);

        this.componentEditorNode = $(this.selectors.editor);

        var toplevelDummyComponent = new Iroha.ComponentEditor.Component('type', 'id', 'name', '@Generic, @Container', '', '', '');
        var addComponentBar = this.createAddComponentBar(toplevelDummyComponent);
        addComponentBar.appendTo(this.componentEditorNode);

        // TODO: sortable にする位置 (componentFrame共通化?)
        this.componentEditorNode.find(".component-container" ).sortable({ axis: 'y' });

        this.componentEditorNode.click($.proxy(function (e) {
            this.componentEditorNode.find('.components-list').slideUp('fast');
        }, this));
    }

    , addComponent: function(id, parentComponent) {
        /// <summary>
        ///
        /// </summary>
        var component = Iroha.ComponentEditor.allComponentsById[id];
        var content = (component.type == "Container" || component.type == "Children") ? "" : component.uiTemplate;
        var componentFrame = $((component.type == "Container" || component.type == "Children") ? component.uiTemplate : $('#template-component-frame').text());
        componentFrame.addClass('component-type-'+component.type);
        componentFrame.attr('data-iroha-component-id', id);
        componentFrame.data('iroha-component-id', id);
        componentFrame.find('.component-name').text(component.name);
        componentFrame.find('.component-container').append(content);
        componentFrame.find('.component-command-remove').click(function (e) { e.preventDefault(); var node = $(this).parents('.component').eq(0).fadeOut('fast', function () { node.remove(); }); });

        if (component.contentTypeMap.length > 0) {
            var childComponentBar = this.createAddComponentBar(component);
            childComponentBar.appendTo(componentFrame);
        }

        parentComponent.find('.component-container').eq(0).append(componentFrame);

        // TODO: sortable にする位置 (componentFrame共通化?)
        componentFrame.find(".component-container" ).sortable({ axis: 'y' });

        return componentFrame;
    }

    , createAddComponentBar: function(parentComponent) {
        /// <summary>
        ///
        /// </summary>
        var addComponentBar = $($("#template-add-component-bar").text());
        var components = Iroha.ComponentEditor.getContainableComponents(parentComponent);

        addComponentBar.find('.add-component-command').click(function (e) {
            e.preventDefault();
            e.stopPropagation(); // component-editorのclickでハンドルしている処理に渡らないように。
            $('.components-list', addComponentBar).slideDown('fast');
        });

        var addComponentsList = $('.components-list ul', addComponentBar);
        var self = this;
        components.filter(function (e) { return !e.isHidden; }).forEach(function (e, i) {
            var liNode = $('<li><a href="#"></a></li>');
            liNode.find('a')
                .data('iroha-component-id', e.id)
                .click(function (e) {
                    e.preventDefault();

                    var id = $(this).data('iroha-component-id');
                    var parentComponent = $(this).parents('.component').eq(0);
                    self.addComponent(id, parentComponent).hide().fadeIn('fast');

                    $(this).parents('.components-list').slideUp('fast');
                })
                .text(e.name);
            addComponentsList.append(liNode);
        });

        return addComponentBar;
    }

    , rebuildFromHtml: function (htmlFragment) {
        /// <summary>
        ///
        /// </summary>
        var contentData = Iroha.ComponentEditor.createContentDataFromHtml(htmlFragment);
        this.rebuildFromContentData(contentData);
    }

    , rebuildFromContentData: function (contentData) {
        /// <summary>
        ///
        /// </summary>
        var parentComponent = this.componentEditorNode;
        parentComponent.find('.component').remove();

        var self = this;
        function addComponentFromContentData(contentData, parentComponent) {
            contentData.forEach(function (content, i) {
                var createdComponentFrame = self.addComponent(content.id, parentComponent);
                content.data.forEach(function (data, j) {
                    createdComponentFrame.find('[data-iroha-component-content='+data.id+']').val(data.content);
                });
                createdComponentFrame
                if (content.children.length > 0) {
                    addComponentFromContentData(content.children, createdComponentFrame);
                }
            });
        }

        addComponentFromContentData(contentData, parentComponent);
    }

    , toContentData: function () {
        /// <summary>
        /// 
        /// </summary>
        return Iroha.ComponentEditor.createContentDataFromDocumentModel(this.componentEditorNode);
    }
    , toHtml: function (finalize) {
        /// <summary>
        ///
        /// </summary>
        return Iroha.ComponentEditor.createHtmlFromContentData(this.toContentData(), finalize);
    }
}
})(window);